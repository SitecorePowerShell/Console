using System.Collections.Generic;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.Core.Settings.Authorization
{
    public class SimpleAuthenticationProvider : ISpeAuthenticationProvider
    {
        public string SharedSecret { get; set; }
        public List<string> AllowedIssuers { get; set; }
        public List<string> AllowedAudiences { get; set; }
        
        public SimpleAuthenticationProvider()
        {
            AllowedIssuers = new List<string>();
            AllowedAudiences = new List<string>();
        }
    }
}