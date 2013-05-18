using System;
using System.Collections;
using System.Globalization;
using System.Management.Automation;
using Cognifide.PowerShell.Shell.Commands.Interactive.Messages;
using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.Shell.Commands.Interactive
{
    [Cmdlet(VerbsCommon.Show, "Input", SupportsShouldProcess = true, DefaultParameterSetName = "Prompt")]
    public class ShowInputCommand : BaseShellCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string Prompt { get; set; }

        [Parameter]
        public string DefaultValue { get; set; }

        [Parameter]
        public string Validation { get; set; }

        [Parameter]
        public string ErrorMessage { get; set; }

        [Parameter]
        public int MaxLength { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
                {
                    if (!string.IsNullOrEmpty(Validation) || MaxLength > 0)
                    {
                        JobContext.MessageQueue.PutMessage(new PromptMessage(Prompt, DefaultValue ?? "",
                                                                             Validation ?? ".*",
                                                                             ErrorMessage ?? "Invalid format",
                                                                             MaxLength < 1 ? int.MaxValue : MaxLength));
                    }
                    else
                    {
                        JobContext.MessageQueue.PutMessage(new PromptMessage(Prompt, DefaultValue ?? ""));
                    }
                    var alertresult = JobContext.MessageQueue.GetResult() as string;
                    WriteObject(alertresult);

                    /*
                                    var parameters = new Hashtable();
                                    ValidatedAdd(parameters, "te", Prompt);
                                    //ValidatedAdd(parameters, "cp", Title);
                                    ValidatedAdd(parameters, "dv", DefaultValue);
                                    ValidatedAdd(parameters, "vd", Validation);
                                    ValidatedAdd(parameters, "em", ErrorMessage);
                                    ValidatedAdd(parameters, "ml", MaxLength);

                                    string yesnoresult = JobContext.ShowModalDialog(parameters, "GetStringResponse",
                                                                                    Width.ToString(CultureInfo.InvariantCulture),
                                                                                    (Height < 100 ? 150 : Height).ToString(CultureInfo.InvariantCulture));
                                    WriteObject(yesnoresult);
                    */
                });

        }

        private void ValidatedAdd(Hashtable parameters, string paramName, object paramValue)
        {
            if (paramValue != null)
            {
                parameters.Add(paramName, paramValue);
            }
        }
    }
}