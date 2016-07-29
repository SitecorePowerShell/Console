using System.Collections;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Diagnostics;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.Commandlets.Diagnostics
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
            if (o == null) return;
            var str1 = o as string;
            if (str1 != null)
            {
                if (str1.Length <= 0) return;
                LogString(str1);
            }
            else
            {
                IEnumerable enumerable;
                if ((enumerable = o as IEnumerable) != null)
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
                }
                else
                {
                    var str2 = o.ToString();
                    if (str2.Length <= 0) return;
                    LogString(str2);
                }
            }
        }

        private void LogString(string message)
        {
            switch (Log)
            {
                case LogNotificationLevel.Debug:
                    PowerShellLog.Debug(message);
                    break;
                case LogNotificationLevel.Error:
                    PowerShellLog.Error(message);
                    break;
                case LogNotificationLevel.Fatal:
                    PowerShellLog.Fatal(message);
                    break;
                case LogNotificationLevel.Warning:
                    PowerShellLog.Warn(message);
                    break;
                default:
                    PowerShellLog.Info(message);
                    break;
            }

            WriteVerbose(message);
        }

        protected override void ProcessRecord()
        {
            LogObject(Object);
        }
    }
}