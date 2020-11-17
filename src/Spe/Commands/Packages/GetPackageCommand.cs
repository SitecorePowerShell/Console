using System.IO;
using System.Management.Automation;
using Sitecore.Install;
using Sitecore.Install.Serialization;
using Sitecore.IO;
using Spe.Core.Extensions;
using Spe.Core.Utility;
using System.IO.Compression;

namespace Spe.Commands.Packages
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
                var fileName = Path;
                if (string.IsNullOrEmpty(fileName)) return;

                if (!System.IO.Path.IsPathRooted(fileName))
                {
                    fileName = FullPackageProjectPath(fileName);
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

                if (packageProject == null)
                {
                    WriteError(typeof(FileNotFoundException), "Cannot find the package.zip contained within the provided file.",
                        ErrorIds.FieldNotFound, ErrorCategory.ObjectNotFound, null);
                }
                else
                {
                    WriteObject(packageProject, false);    
                }
            });
        }

        private static PackageProject ExtractPackageProject(string fileName)
        {
            PackageProject packageProject = null;

            using (var packageReader = ZipFile.Open(fileName, ZipArchiveMode.Read))
            {
                var packageEntry = packageReader.GetEntry("package.zip");
                if (packageEntry == null)
                {
                    return null;
                }

                using (var memoryStream = new MemoryStream())
                {
                    StreamUtils.CopyStream(packageEntry.Open(), memoryStream, 0x4000);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    using(var reader = new ZipArchive(memoryStream))
                    {
                        foreach (var entry in reader.Entries)
                        {
                            if (!entry.FullName.Is(Constants.ProjectKey)) continue;

                            packageProject = IOUtils.LoadSolution(StreamUtil.LoadString(entry.Open()));
                            break;
                        }
                    }
                }
            }

            return packageProject;
        }
    }
}