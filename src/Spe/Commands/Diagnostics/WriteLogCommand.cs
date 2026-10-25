using System.Collections;
using System.Management.Automation;
using Sitecore.Diagnostics;
using Spe.Core.Diagnostics;

namespace Spe.Commands.Diagnostics
{
    [Cmdlet(VerbsCommunications.Write, "Log")]
    public class WriteLogCommand : BaseCommand
    {
        public WriteLogCommand()
        {
            Separator = " ";
        }

        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromRemainingArguments = true)]
        public object Object { get; set; }

        [Parameter]
        public object Separator { get; set; }

        [Parameter]
        public LogNotificationLevel Log { get; set; }

        private void LogObject(object o)
        {
            switch (o)
            {
                case null:
                case string str1 when str1.Length <= 0:
                    return;
                case string str1:
                    LogString(str1);
                    break;
                case IEnumerable enumerable:
                {
                    var flag = false;
                    foreach (var o1 in enumerable)
                    {
                        if (flag && Separator != null)
                        {
                            LogString(Separator.ToString());
                        }
                        LogObject(o1);
                        flag = true;
                    }

                    break;
                }
                default:
                {
                    var str2 = o.ToString();
                    if (str2.Length <= 0) return;
                    LogString(str2);
                    break;
                }
            }
        }

        private void LogString(string message)
        {
            PowerShellLog.Script(Log, message);
            WriteVerbose(message);
        }

        protected override void ProcessRecord()
        {
            LogObject(Object);
        }
    }
}