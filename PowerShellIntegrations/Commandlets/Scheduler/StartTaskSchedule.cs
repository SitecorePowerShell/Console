using System.Management.Automation;
using Sitecore.Data.Items;
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
        [Alias("FullName", "FileName")]
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