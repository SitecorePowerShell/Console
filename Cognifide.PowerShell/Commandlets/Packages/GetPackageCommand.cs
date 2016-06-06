using System.IO;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Install;
using Sitecore.Install.Serialization;
using Sitecore.IO;
using Sitecore.Zip;
using Sitecore.Zip.Utils;

namespace Cognifide.PowerShell.Commandlets.Packages
{
    [Cmdlet(VerbsCommon.Get, "Package")]
    [OutputType(typeof (PackageProject))]
    public class GetPackageCommand : BasePackageCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            PerformInstallAction(() =>
            {
                PackageProject packageProject = null;
                if (Path != null)
                {
                    var fileName = Path;

                    if (!System.IO.Path.IsPathRooted(Path))
                    {
                        fileName = FullPackageProjectPath(Path);
                    }

                    if (File.Exists(fileName))
                    {
                        var isZip = false;
                        using (var stream = FileUtil.OpenRead(fileName))
                        {
                            if (ZipUtils.IsZipContent(stream))
                            {
                                isZip = true;
                            }
                        }

                        packageProject = !isZip ? IOUtils.LoadSolution(FileUtil.ReadFromFile(fileName)) : ExtractPackageProject(fileName);
                    }

                    WriteObject(packageProject, false);
                }
            });
        }

        private static PackageProject ExtractPackageProject(string fileName)
        {
            PackageProject packageProject = null;

            using (var packageReader = new ZipReader(fileName))
            {
                var packageEntry = packageReader.GetEntry("package.zip");
                using (var memoryStream = new MemoryStream())
                {
                    StreamUtils.CopyStream(packageEntry.GetStream(), memoryStream, 0x4000);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new ZipReader(memoryStream))
                    {
                        foreach (var entry in reader.Entries)
                        {
                            if (!entry.Name.Is(Constants.ProjectKey)) continue;

                            packageProject = IOUtils.LoadSolution(StreamUtil.LoadString(entry.GetStream()));
                            break;
                        }
                    }
                }
            }

            return packageProject;
        }
    }
}