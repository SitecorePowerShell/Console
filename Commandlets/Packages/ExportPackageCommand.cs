using System.Management.Automation;
using Sitecore.Install;
using Sitecore.Install.Serialization;
using Sitecore.Install.Zip;
using Sitecore.IO;
using Sitecore.Shell.Applications.ContentEditor;

namespace Cognifide.PowerShell.Commandlets.Packages
{
    [Cmdlet(VerbsData.Export, "Package", SupportsShouldProcess = true)]
    public class ExportPackageCommand : BasePackageCommand
    {
        [Parameter(Position = 0)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter(Position = 1, ValueFromPipeline = true)]
        public PackageProject Project { get; set; }

        [Parameter]
        public SwitchParameter Zip { get; set; }

        [Parameter]
        public SwitchParameter NoClobber { get; set; }

        [Parameter]
        public SwitchParameter IncludeProject { get; set; }

        protected override void ProcessRecord()
        {
            var fileName = Path;

            if (!Zip.IsPresent)
            {
                PerformInstallAction(() =>
                {
                    if (fileName == null)
                    {
                        //name of the zip file when not defined                        
                        fileName = string.Format(
                            "{0}{1}{2}.xml",
                            Project.Metadata.PackageName,
                            string.IsNullOrEmpty(Project.Metadata.Version) ? "" : "-",
                            Project.Metadata.Version);
                    }

                    if (!System.IO.Path.IsPathRooted(fileName))
                    {
                        fileName = FullPackageProjectPath(fileName);
                    }

                    if (!System.IO.Path.HasExtension(fileName))
                    {
                        fileName = fileName + ".xml";
                    }

                    if (NoClobber.IsPresent)
                    {
                        fileName = GetTargetFileName(fileName);
                    }

                    if (ShouldProcess(fileName, "Export package project"))
                    {
                        FileUtil.WriteToFile(fileName, IOUtils.StoreObject(Project));
                    }
                });
            }
            else
            {
                PerformInstallAction(
                    () =>
                    {
                        if (IncludeProject.IsPresent)
                        {
                            Project.SaveProject = true;
                        }

                        if (fileName == null)
                        {
                            //name of the zip file when not defined
                            fileName = string.Format(
                                "{0}-PS-{1}.zip", Project.Metadata.PackageName, Project.Metadata.Version);
                        }

                        if (!System.IO.Path.IsPathRooted(fileName))
                        {
                            fileName = FullPackagePath(fileName);
                        }

                        if (!System.IO.Path.HasExtension(fileName))
                        {
                            fileName = fileName + ".zip";
                        }

                        if (NoClobber.IsPresent)
                        {
                            fileName = GetTargetFileName(fileName);
                        }

                        if (ShouldProcess(fileName, "Export package"))
                        {
                            using (var writer = new PackageWriter(fileName))
                            {
                                writer.Initialize(Installer.CreateInstallationContext());
                                PackageGenerator.GeneratePackage(Project, writer);
                            }
                        }
                    });
            }
        }

        public string GetTargetFileName(string filename)
        {
            var directory = System.IO.Path.GetDirectoryName(filename);
            var name = System.IO.Path.GetFileNameWithoutExtension(filename);
            var extension = System.IO.Path.GetExtension(filename);
            var path = System.IO.Path.Combine(directory, (name + extension));

            var num = 1;
            while (System.IO.File.Exists(path))
            {
                path = System.IO.Path.Combine(directory, (name + " (" + num + ")" + extension));
            }

            return path;
        }
    }
}