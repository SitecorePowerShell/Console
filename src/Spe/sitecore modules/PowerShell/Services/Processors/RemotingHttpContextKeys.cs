namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    // String keys for the per-request HttpContext.Items slots used to thread
    // remoting state across the handler pipeline. Centralised so every
    // processor reads/writes the same key without re-typing the literal.
    // Invariant: a `Spe(RemotingClient|TokenResult|AuthKind|ClientIp|RequestId)`
    // string literal anywhere in this assembly outside this file is a bug;
    // grep for it before merging refactors that touch this surface.
    internal static class RemotingHttpContextKeys
    {
        public const string RemotingClient = "SpeRemotingClient";
        public const string TokenResult = "SpeTokenResult";
        public const string AuthKind = "SpeAuthKind";
        public const string ClientIp = "SpeClientIp";
        public const string RequestId = "SpeRequestId";
    }
}
