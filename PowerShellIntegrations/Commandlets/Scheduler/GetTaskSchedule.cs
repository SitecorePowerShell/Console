using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Tasks;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Scheduler
{
    [Cmdlet(VerbsCommon.Get, "TaskSchedule", DefaultParameterSetName = "From Path")]
    [OutputType(new[] {typeof (ScheduleItem)})]
    public class GetTaskSchedule : DatabaseContextBaseCommand
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
                var schedule = new ScheduleItem(Item);
                WriteObject(schedule);
            }
            else if (Path != null)
            {
                Item curItem = PathUtilities.GetItem(Path, CurrentDrive, CurrentPath);
                var schedule = new ScheduleItem(curItem);
                WriteObject(schedule);
            }
            else
            {
                base.ProcessRecord();
            }
        }

        protected override void ProcessRecord(IEnumerable<Database> databases)
        {
            if (string.IsNullOrEmpty(Name))
            {
                foreach (var database in databases)
                {
                    Item[] taskItems =
                        database.SelectItems("/sitecore/system/Tasks/Schedules//*[@@templatename='Schedule']");
                    foreach (var taskItem in taskItems)
                    {
                        WriteObject(new ScheduleItem(taskItem));
                    }
                }
            }
            else
            {
                foreach (var database in databases)
                {
                    IEnumerable<ScheduleItem> taskItems =
                        database.SelectItems("/sitecore/system/Tasks/Schedules//*[@@templatename='Schedule']")
                            .Select(item => new ScheduleItem(item));
                    WildcardWrite(Name, taskItems, task => task.Name);
                }
            }
        }
    }
}