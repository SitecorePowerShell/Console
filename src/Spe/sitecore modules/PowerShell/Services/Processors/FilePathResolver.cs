using System;
using System.IO;
using Sitecore;
using Sitecore.IO;
using Spe.Core.Settings.Authorization;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal static class FilePathResolver
    {
        // Resolves the on-disk file path for fileDownload/fileUpload. Returns
        // null when the request must be rejected; rejectReason names the audit
        // code. Two cases:
        //   1. Origin matches a built-in alias (data, log, media, ...): combine
        //      alias root + relative path, canonicalize, require canonical
        //      path to stay under the alias root. Catches encoded traversal
        //      that a simple ".." string check would miss.
        //   2. Origin is unknown (or empty): only honored when pathParam is an
        //      absolute path AND that path canonicalizes under one of the
        //      Spe.Remoting.AllowedFileRoots entries. Default allowlist is
        //      empty so unknown-origin absolute paths are rejected (#1443).
        public static string GetPathFromParameters(string originParam, string pathParam, out string rejectReason)
        {
            rejectReason = null;

            if (OriginAliasFolders.TryGetFolder(originParam, out var aliasFolder))
            {
                return ResolveUnderRoot(aliasFolder, pathParam, out rejectReason);
            }

            if (string.IsNullOrEmpty(pathParam) || !Path.IsPathRooted(pathParam))
            {
                rejectReason = "unknownOrigin";
                return null;
            }

            return ResolveAgainstAllowlist(pathParam, out rejectReason);
        }

        // Canonicalize a path. Returns true on success; on failure sets reason
        // to "invalidPath" so callers can surface a uniform audit code.
        // Sitecore-relative paths (e.g. "/App_Data" from Settings.DataFolder)
        // are routed through FileUtil.MapPath. Path.IsPathRooted alone is not
        // enough - it returns true for "/App_Data" on Windows, which would
        // misroute the alias to "C:\App_Data" via Path.GetFullPath instead of
        // the real "C:\inetpub\wwwroot\App_Data".
        private static bool TryCanonicalize(string input, out string canonical, out string rejectReason)
        {
            try
            {
                canonical = WebServiceSettings.HasExplicitDriveOrUnc(input)
                    ? Path.GetFullPath(input)
                    : Path.GetFullPath(FileUtil.MapPath(StringUtil.EnsurePostfix('/', input)));
                rejectReason = null;
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                canonical = null;
                rejectReason = "invalidPath";
                return false;
            }
        }

        // True when canonical is exactly rootAbsolute or sits beneath it.
        // Caller must pass already-canonicalized inputs (no `..`, no trailing
        // separator ambiguity). Compares with a separator suffix so siblings
        // can't match (e.g. "C:\Data" must not match "C:\DataLeak").
        private static bool IsUnderRoot(string canonical, string rootAbsolute)
        {
            if (string.Equals(canonical, rootAbsolute, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var rootWithSep = rootAbsolute.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            return canonical.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveUnderRoot(string folder, string pathParam, out string rejectReason)
        {
            if (!TryCanonicalize(folder, out var rootAbsolute, out rejectReason))
            {
                return null;
            }

            if (string.IsNullOrEmpty(pathParam))
            {
                return rootAbsolute;
            }

            string canonical;
            try
            {
                canonical = Path.GetFullPath(Path.Combine(rootAbsolute, pathParam.TrimStart('/', '\\')));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                rejectReason = "invalidPath";
                return null;
            }

            if (!IsUnderRoot(canonical, rootAbsolute))
            {
                rejectReason = "outsideRoot";
                return null;
            }

            return canonical;
        }

        private static string ResolveAgainstAllowlist(string pathParam, out string rejectReason)
        {
            var allowedRoots = WebServiceSettings.AllowedFileRoots;
            if (allowedRoots == null || allowedRoots.Length == 0)
            {
                rejectReason = "unknownOrigin";
                return null;
            }

            if (!TryCanonicalize(pathParam, out var canonical, out rejectReason))
            {
                return null;
            }

            // AllowedFileRoots entries are pre-canonicalized at startup
            // (WebServiceSettings.ParseAllowedFileRoots), so each is already
            // an absolute path. Reuse the same comparator the alias path uses.
            foreach (var root in allowedRoots)
            {
                if (IsUnderRoot(canonical, root))
                {
                    return canonical;
                }
            }

            rejectReason = "outsideRoot";
            return null;
        }
    }
}
