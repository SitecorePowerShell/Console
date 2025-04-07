using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Pipelines.Upload;
using Sitecore.StringExtensions;

namespace Spe.Client.Applications.UploadFile.Validation
{
    internal class ContentTypeValidator
    {
        internal IReadOnlyCollection<FileTypeValidator> validators { get; }

        public ContentTypeValidator(string[] patterns)
        {
            validators = CreateValidators(patterns);
        }

        public ValidationResult Validate(HttpFileCollection Files)
        {
            if (!validators.Any())
            {
                return new ValidationResult { Message = string.Empty, Valid = true };
            }

            foreach (string key in Files)
            {
                var file = Files[key];

                if (file == null)
                {
                    continue;
                }

                if (!IsFileAccepted(file, validators))
                {
                    var reason = Translate.Text("File type isn`t allowed.");
                    reason = StringUtil.EscapeJavascriptString(reason);
                    var convertedFileName = StringUtil.EscapeJavascriptString(file.FileName);

                    var errorText = Translate.Text(string.Format("The '{0}' file cannot be uploaded. File type isn`t allowed.", file.FileName));
                    Log.Warn(errorText, this);
                    return new ValidationResult { Message = errorText, Valid = false };
                }
            }
            return new ValidationResult { Message = string.Empty, Valid = true };
        }

        protected static bool IsUnpack(HttpPostedFileBase file)
        {
            return string.Compare(Path.GetExtension(file.FileName), ".zip", StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        private static bool IsFileAccepted(HttpPostedFile file, IReadOnlyCollection<FileTypeValidator> validators)
        {
            if (string.IsNullOrEmpty(file.FileName))
            {
                return true;
            }

            var isArchive = IsUnpack(new HttpPostedFileWrapper(file));
            if (!isArchive)
            {
                return validators.Any(x => x.IsValid(file.FileName));
            }


            if (file.InputStream.Position != 0)
            {
                file.InputStream.Position = 0;
            }

            var archive = new ZipArchive(file.InputStream, ZipArchiveMode.Read, true);
            try
            {
                return archive.Entries
                    .Where(entry => !entry.FullName.EndsWith("/"))
                    .All(entry => validators.Any(x => x.IsValid(entry.FullName)));
            }
            finally
            {
                archive.Dispose();
                if (file.InputStream.Position != 0)
                {
                    file.InputStream.Position = 0;
                }
            }
        }

        private static IReadOnlyCollection<FileTypeValidator> CreateValidators(string[] allowedFileTypes)
        {
            if (!allowedFileTypes.Any())
            {
                return new List<FileTypeValidator>();
            }

            return allowedFileTypes
                .Select(p => p.Trim())
                .Select(p => new FileTypeValidator(p))
                .ToList();
        }
    }
}
