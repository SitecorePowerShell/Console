using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Data;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Exceptions;
using Sitecore.Tasks;

namespace Cognifide.PowerShell.Commandlets.Scheduler
{
    [Cmdlet(VerbsCommon.Get, "TaskSchedule", DefaultParameterSetName = "From Database and Name")]
    [OutputType(typeof (ScheduleItem))]
    public class GetTaskScheduleCommand : DatabaseContextBaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "From Item",
            Mandatory = true)]
        public Item Item { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "From Path",
            Mandatory = true)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            if (Item != null)
            {
                if (CheckItemTypeMatch(Item))
                {
                    var schedule = new ScheduleItem(Item);
                    WriteObject(schedule);
                }
            }
            else if (Path != null)
            {
                var curItem = PathUtilities.GetItem(Path, CurrentDrive, CurrentPath);
                if (CheckItemTypeMatch(curItem))
                {
                    var schedule = new ScheduleItem(curItem);
                    WriteObject(schedule);
                }
            }
            else
            {
                base.ProcessRecord();
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

        protected override void ProcessRecord(IEnumerable<Database> databases)
        {
            if (string.IsNullOrEmpty(Name))
            {
                foreach (
                    var taskItem in
                        databases.Select(
                            database =>
                                database.SelectItems("/sitecore/system/Tasks/Schedules//*[@@templatename='Schedule']"))
                            .SelectMany(taskItems => taskItems))
                {
                    WriteObject(new ScheduleItem(taskItem));
                }
            }
            else
            {
                foreach (
                    var taskItems in
                        databases.Select(
                            database =>
                                database.SelectItems("/sitecore/system/Tasks/Schedules//*[@@templatename='Schedule']")
                                    .Select(item => new ScheduleItem(item))))
                {
                    WildcardWrite(Name, taskItems, task => task.Name);
                }
            }
        }
    }
}