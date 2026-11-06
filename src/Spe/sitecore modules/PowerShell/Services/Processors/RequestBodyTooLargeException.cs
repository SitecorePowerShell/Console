using System;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    // Thrown when the decoded request body would exceed
    // WebServiceSettings.MaxScriptBodySize. ASP.NET's
    // maxRequestLength bounds the compressed POST only; this is the
    // bound on the decoded side. Translated to 413 at the call site.
    internal sealed class RequestBodyTooLargeException : Exception
    {
        public RequestBodyTooLargeException(int cap)
            : base($"Request body exceeds the configured cap of {cap} bytes.") { }
    }
}
