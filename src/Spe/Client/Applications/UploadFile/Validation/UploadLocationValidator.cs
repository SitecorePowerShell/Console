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
    internal class UploadLocationValidator
    {
        private readonly List<string> _allowedLocations;
        private readonly string _webRootPath;

        public UploadLocationValidator(IEnumerable<string> allowedLocations)
        {
            _webRootPath = HttpContext.Current.Server.MapPath("\\");

            // Convert relative paths to absolute paths
            _allowedLocations = allowedLocations
                .Select(path => Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(_webRootPath, path)))
                .ToList();
        }

        public bool Validate(string userDefinedPath)
        {
            if (string.IsNullOrWhiteSpace(userDefinedPath)) return false;

            string fullPath;
            try
            {
                fullPath = GetFullPath(userDefinedPath);
            }
            catch (Exception)
            {
                return false; // Invalid path format
            }

            return _allowedLocations.Any(allowedPath => fullPath.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase));
        }

        public string GetFullPath(string path)
        {
            return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(_webRootPath, path));
        }
    }
}
