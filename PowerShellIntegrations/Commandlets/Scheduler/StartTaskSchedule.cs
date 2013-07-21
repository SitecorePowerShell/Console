using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Security.Accounts;
using Sitecore.Security.Authentication;
using Sitecore.Tasks;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Scheduler
{
    [Cmdlet("Start", "TaskSchedule")]
    [OutputType(new[] { typeof(ScheduleItem) })]
    public class StartTaskSchedule : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromSchedule", Mandatory = true)]
        public ScheduleItem Schedule { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromItem", Mandatory = true)]
        public Item Item { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromPath", Mandatory = true)]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            ScheduleItem schedule = null;
            if (Item != null)
            {
                schedule = new ScheduleItem(Item);
            }

            if (Schedule != null)
            {
                schedule = Schedule;
            }

            if (Path != null)
            {
                var curItem = PathUtilities.GetItem(Path, CurrentDrive, CurrentPath);
                schedule = new ScheduleItem(curItem);
            }

            if (schedule != null)
            {
                schedule.Execute();
            }
            WriteObject(schedule);
        }
    }
}