using System;
using Sitecore.Globalization;
using Sitecore.Security.Accounts;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface IJobOptions
    {
        string JobName { get; set; }
        User ContextUser { get; set; }
        bool EnableSecurity { get; set; }
        Language ClientLanguage { get; set; }
        bool AtomicExecution { get; set; }
        TimeSpan AfterLife { get; set; }
        bool WriteToLog { get; set; }
        string SiteName { get; set; }
    }
}