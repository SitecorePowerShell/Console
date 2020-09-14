using System.Collections.Generic;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface ISpeAuthenticationProvider
    {
        string SharedSecret { get; set; }
        List<string> AllowedIssuers { get; set; }
        List<string> AllowedAudiences { get; set; }
    }
}
