using System.Collections;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.Utility
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

        private void LogString(string logMessage)
        {
            switch (Log)
            {
                case LogNotificationLevel.Debug:
                    Sitecore.Diagnostics.Log.Debug(logMessage, this);
                    break;
                case LogNotificationLevel.Error:
                    Sitecore.Diagnostics.Log.Error(logMessage, this);
                    break;
                case LogNotificationLevel.Fatal:
                    Sitecore.Diagnostics.Log.Fatal(logMessage, this);
                    break;
                case LogNotificationLevel.Warning:
                    Sitecore.Diagnostics.Log.Warn(logMessage, this);
                    break;
                    //case (LogNotificationLevel.Info) :
                    //case LogNotificationLevel.None:
                default:
                    Sitecore.Diagnostics.Log.Info(logMessage, this);
                    break;
            }

            WriteVerbose(logMessage);
        }

        protected override void ProcessRecord()
        {
            LogObject(Object);
        }
    }
}