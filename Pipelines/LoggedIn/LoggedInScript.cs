﻿using Cognifide.PowerShell.PowerShellIntegrations.Modules;
using Sitecore.Pipelines.LoggedIn;

namespace Cognifide.PowerShell.Pipelines.LoggedIn
{
    public class LoggedInScript : PipelineProcessor<LoggedInArgs>
    {
        protected override string IntegrationPoint { get { return IntegrationPoints.PipelineLoggedInFeature; } }
    }
}