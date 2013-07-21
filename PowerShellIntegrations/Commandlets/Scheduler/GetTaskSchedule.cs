using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Tasks;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Scheduler
{
    [Cmdlet(VerbsCommon.Get, "TaskSchedule", DefaultParameterSetName = "DatabaseName")]
    [OutputType(new[] { typeof(ScheduleItem) })]
    public class GetTaskSchedule : DatabaseContextBaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromItem", Mandatory = true)]
        public Item Item { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromPath", Mandatory = true)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            if (Item != null)
            {
                ScheduleItem schedule = new ScheduleItem(Item);
                WriteObject(schedule);
            }
            else if (Path != null)
            {
                var curItem = PathUtilities.GetItem(Path, CurrentDrive, CurrentPath);
                ScheduleItem schedule = new ScheduleItem(curItem);
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
                foreach (Database database in databases)
                {
                    var taskItems = database.SelectItems("/sitecore/system/Tasks/Schedules//*[@@templatename='Schedule']");
                    foreach (var taskItem in taskItems)
                    {
                        WriteObject(new ScheduleItem(taskItem));
                    }
                }
            }
            else
            {
                foreach (Database database in databases)
                {
                    var taskItems =
                        database.SelectItems("/sitecore/system/Tasks/Schedules//*[@@templatename='Schedule']").Select(item => new ScheduleItem(item));
                    WildcardWrite(Name, taskItems, task => task.Name);
                }
            }
        }
    }
}