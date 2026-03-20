namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public class TokenValidationResult
    {
        public string Scope { get; set; }
        public string ClientSessionId { get; set; }
    }
}
