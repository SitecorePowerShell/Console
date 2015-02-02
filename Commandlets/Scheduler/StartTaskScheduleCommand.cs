using System.Management.Automation;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Exceptions;
using Sitecore.Tasks;

namespace Cognifide.PowerShell.Commandlets.Scheduler
{
    [Cmdlet("Start", "TaskSchedule")]
    [OutputType(new[] {typeof (ScheduleItem)})]
    public class StartTaskScheduleCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromSchedule",
            Mandatory = true)]
        public ScheduleItem Schedule { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromItem",
            Mandatory = true)]
        public Item Item { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromPath",
            Mandatory = true)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            ScheduleItem schedule = null;
            if (Item != null)
            {
                if (!CheckItemTypeMatch(Item))
                {
                    return;
                }
                schedule = new ScheduleItem(Item);
            }

            if (Schedule != null)
            {
                schedule = Schedule;
            }

            if (Path != null)
            {
                Item curItem = PathUtilities.GetItem(Path, CurrentDrive, CurrentPath);
                if (!CheckItemTypeMatch(curItem))
                {
                    return;
                }
                schedule = new ScheduleItem(curItem);
            }

            if (schedule != null)
            {
                schedule.Execute();
            }
            WriteObject(schedule);
        }

        private bool CheckItemTypeMatch(Item item)
        {
            if (!TemplateManager.GetTemplate(item).DescendsFromOrEquals(Sitecore.TemplateIDs.Schedule))
            {
                WriteError(new ErrorRecord(new InvalidTypeException("Item is not of template or rerived from template 'Schedule'"), "sitecore_template_is_not_schedule", ErrorCategory.InvalidType, item));
                return false;
            }
            return true;
        }


    }
}