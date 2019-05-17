using System;
using Sitecore.Globalization;

namespace Cognifide.PowerShell.Services
{
    public interface IJobOptions
    {
        string JobName { get; set; }
        Sitecore.Security.Accounts.User ContextUser { get; set; }
        bool EnableSecurity { get; set; }
        Language ClientLanguage { get; set; }
        bool AtomicExecution { get; set; }
        TimeSpan AfterLife { get; set; }
        bool WriteToLog { get; set; }
        string SiteName { get; set; }
    }
}
