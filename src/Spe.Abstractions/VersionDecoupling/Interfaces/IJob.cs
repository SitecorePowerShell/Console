﻿using System.Collections.Specialized;
using Sitecore;
using Sitecore.Jobs.AsyncUI;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface IJob
    {
        Handle Handle { get; }
        MessageQueue MessageQueue { get; }
        string Name { get; }

        bool StatusFailed { get; set; }

        object StatusResult { get; set; }

        void AddStatusMessage(string message);

        StringCollection StatusMessages { get; }

        IJobOptions Options { get; }

        bool IsDone { get; }
    }
}