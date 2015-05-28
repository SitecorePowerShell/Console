using System.Management.Automation;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Exceptions;
using Sitecore.Tasks;

namespace Cognifide.PowerShell.Commandlets.Scheduler
{
    [Cmdlet("Start", "TaskSchedule", SupportsShouldProcess = true)]
    [OutputType(typeof (ScheduleItem))]
    public class StartTaskScheduleCommand : BaseLanguageAgnosticItemCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromSchedule",
            Mandatory = true)]
        public ScheduleItem Schedule { get; set; }

        protected override void ProcessRecord()
        {
            if (Schedule != null)
            {
                ProcessItem(null);
            }
            else
            {
                base.ProcessRecord();
            }
        }

        protected override void ProcessItem(Item item)
        {
            ScheduleItem schedule = null;
            if (item != null)
            {
                if (!CheckItemTypeMatch(item))
                {
                    return;
                }
                schedule = new ScheduleItem(item);
            }

            if (Schedule != null)
            {
                schedule = Schedule;
            }

            if (schedule != null)
            {
                if (ShouldProcess(item.GetProviderPath(), "Start task defined in schedule"))
                {
                    schedule.Execute();
                    WriteObject(schedule);
                }
            }
        }

        private bool CheckItemTypeMatch(Item item)
        {
            if (!TemplateManager.GetTemplate(item).DescendsFromOrEquals(TemplateIDs.Schedule))
            {
                WriteError(
                    new ErrorRecord(
                        new InvalidTypeException("Item is not of template or rerived from template 'Schedule'"),
                        "sitecore_template_is_not_schedule", ErrorCategory.InvalidType, item));
                return false;
            }
            return true;
        }
    }
}