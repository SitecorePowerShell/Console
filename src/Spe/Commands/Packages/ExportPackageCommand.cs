﻿using System.IO;
using System.Management.Automation;
using Sitecore.Install;
using Sitecore.Install.Serialization;
using Sitecore.Install.Zip;
using Sitecore.IO;

namespace Spe.Commands.Packages
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

        private static bool ContainsInvalidPathCharacters(string filePath)
        {
            for (var i = 0; i < filePath.Length; i++)
            {
                int c = filePath[i];

                if (c == '\"' || c == '<' || c == '>' || c == '|' || c == '*' || c == '?' || c < 32)
                    return true;
            }

            return false;
        }

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
                        fileName = $"{Project.Metadata.PackageName}{(string.IsNullOrEmpty(Project.Metadata.Version) ? "" : "-")}{Project.Metadata.Version}{Constants.SolutionExtension}";
                    }

                    if (!System.IO.Path.IsPathRooted(fileName))
                    {
                        fileName = FullPackageProjectPath(fileName);
                    }

                    if (!System.IO.Path.HasExtension(fileName))
                    {
                        fileName = fileName + Constants.SolutionExtension;
                    }

                    if (NoClobber.IsPresent && File.Exists(fileName))
                    {
                        WriteError(typeof(IOException), $"The file '{fileName}' already exists.",
                            ErrorIds.FileAlreadyExists, ErrorCategory.ResourceExists, fileName);
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
                            fileName = $"{Project.Metadata.PackageName}-PS-{Project.Metadata.Version}{Constants.PackageExtension}";
                        }

                        if (ContainsInvalidPathCharacters(fileName))
                        {
                            WriteError(typeof(IOException), "The file specified contains invalid characters in the name.",
                                ErrorIds.FileNameInvalid, ErrorCategory.InvalidArgument, fileName);
                        }

                        if (!System.IO.Path.IsPathRooted(fileName))
                        {
                            fileName = FullPackagePath(fileName);
                        }

                        if (!System.IO.Path.HasExtension(fileName))
                        {
                            fileName = fileName + Constants.PackageExtension;
                        }

                        if (NoClobber.IsPresent && File.Exists(fileName))
                        {
                            WriteError(typeof(IOException), $"The file '{fileName}' already exists.",
                                ErrorIds.FileAlreadyExists, ErrorCategory.ResourceExists, fileName);
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
    }
}