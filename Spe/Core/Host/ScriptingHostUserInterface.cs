using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Commands.Interactive.Messages;
using Spe.Core.Diagnostics;
using Spe.Core.Settings;
using Spe.Core.VersionDecoupling;

namespace Spe.Core.Host
{
    public class ScriptingHostUserInterface : PSHostUserInterface, IHostUISupportsMultipleChoiceSelection
    {
        private readonly ScriptingHostRawUserInterface rawUi;
        private readonly ScriptingHost host;
        private ScriptingHostPrivateData privateData;

        private ScriptingHostPrivateData PrivateData
        {
            get
            {
                if (privateData == null)
                {
                    privateData = host.PrivateData.BaseObject as ScriptingHostPrivateData;
                }
                return privateData;
            }
        }

        public ScriptingHostUserInterface(ApplicationSettings settings, ScriptingHost host)
        {
            rawUi = new ScriptingHostRawUserInterface(settings);
            this.host = host;
        }

        /// <summary>
        ///     A reference to the PSHost implementation.
        /// </summary>
        public OutputBuffer Output => rawUi.Output;

        public override PSHostRawUserInterface RawUI => rawUi;

        public override string ReadLine()
        {
            var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
            var job = jobManager.GetContextJob();
            if (job == null || !CheckSessionCanDoInteractiveAction(nameof(ReadLine)))
                throw new NotImplementedException();

            var options = new object[1];
            options[0] = new Hashtable()
            {
                ["Title"] = " ",
                ["Name"] = "varString",
                ["Value"] = string.Empty
            };
            job.MessageQueue.PutMessage(new ShowMultiValuePromptMessage(options, "600", "200",
                "Sitecore PowerShell Extensions", " ", string.Empty, string.Empty, string.Empty, false, null, null,
                host.SessionKey));
            var values = (object[])job.MessageQueue.GetResult() ?? new object[] { string.Empty };
            return ((Hashtable)values[0])["Value"] as string;
        }

        public override SecureString ReadLineAsSecureString()
        {
            var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
            var job = jobManager.GetContextJob();
            if (job == null || !CheckSessionCanDoInteractiveAction(nameof(ReadLineAsSecureString)))
                throw new NotImplementedException();

            var options = new object[1];
            options[0] = new Hashtable()
            {
                ["Title"] = " ",
                ["Name"] = "varSecure",
                ["Value"] = string.Empty,
                ["Editor"] = "password"
            };
            job.MessageQueue.PutMessage(new ShowMultiValuePromptMessage(options, "600", "200",
                "Sitecore PowerShell Extensions", " ", string.Empty, string.Empty, string.Empty, false, null, null,
                host.SessionKey));
            var values = (object[]) job.MessageQueue.GetResult() ?? new object[] {string.Empty};

            return ToSecureString(((Hashtable)values[0])["Value"] as string);
        }

        public override void Write(string value)
        {
            var lastline = Output.LastOrDefault();
            if (!(lastline?.Terminated ?? true))
            {
                lastline.Text += value;
                if (value.EndsWith("\n"))
                {
                    lastline.Terminated = true;
                }
            }
            else
            {
                var splitter = new BufferSplitterCollection(OutputLineType.Output, value, RawUI, false);
                Output.AddRange(splitter);
            }
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Output, value, RawUI.BufferSize.Width,
                foregroundColor,
                backgroundColor, false);
            Output.AddRange(splitter);
        }

        public override void WriteLine(string value)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Output, value, RawUI, true);
            Output.AddRange(splitter);
        }

        public override void WriteErrorLine(string value)
        {
            
            var splitter = new BufferSplitterCollection(OutputLineType.Error, value, RawUI.BufferSize.Width,
                PrivateData.ErrorForegroundColor,
                PrivateData.ErrorBackgroundColor, true);
            Output.HasErrors = true;
            Output.AddRange(splitter);
        }

        public override void WriteDebugLine(string message)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Debug, "DEBUG: " + message,
                RawUI.WindowSize.Width,
                PrivateData.DebugForegroundColor, PrivateData.DebugBackgroundColor, true);
            Output.AddRange(splitter);
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            if (!CheckSessionCanDoInteractiveAction($"{nameof(WriteProgress)}:{record.Activity}/{record.CurrentOperation} ({record.PercentComplete}%)", false)) return;
            var message = Message.Parse(this, "ise:updateprogress");
            message.Arguments.Add("Activity", record.Activity);
            message.Arguments.Add("ActivityId", record.ActivityId.ToString(CultureInfo.InvariantCulture));
            message.Arguments.Add("CurrentOperation", record.CurrentOperation);
            message.Arguments.Add("StatusDescription", record.StatusDescription);
            message.Arguments.Add("ParentActivityId", record.ParentActivityId.ToString(CultureInfo.InvariantCulture));
            message.Arguments.Add("PercentComplete", record.PercentComplete.ToString(CultureInfo.InvariantCulture));
            message.Arguments.Add("RecordType", record.RecordType.ToString());
            message.Arguments.Add("SecondsRemaining", record.SecondsRemaining.ToString(CultureInfo.InvariantCulture));
            var sheerMessage = new SendMessageMessage(message, false);
            
            var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
            var job = jobManager.GetContextJob();
            if (job != null)
            {
                message.Arguments.Add("JobId", job.Name);
                job.MessageQueue.PutMessage(sheerMessage);
            }
            else
            {
                sheerMessage.Execute();
            }
        }

        public override void WriteVerboseLine(string message)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Verbose, "VERBOSE: " + message, RawUI.WindowSize.Width,
                PrivateData.VerboseForegroundColor, PrivateData.VerboseBackgroundColor, true);
            Output.AddRange(splitter);
        }

        public override void WriteWarningLine(string message)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Warning, "WARNING: " + message, RawUI.BufferSize.Width,
                PrivateData.WarningForegroundColor, PrivateData.WarningBackgroundColor, true);
            Output.AddRange(splitter);
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message,
            Collection<FieldDescription> descriptions)
        {
            var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
            var job = jobManager.GetContextJob();

            if (job == null || !CheckSessionCanDoInteractiveAction(nameof(Prompt))) throw new NotImplementedException();
            var options = new object[descriptions.Count];
            for (var i=0 ; i < descriptions.Count; i++)
            {
                var description = descriptions[i];
                string editor = description.ParameterTypeName.Contains("SecureString") ? "password" : "string";
                options[i] = new Hashtable()
                {
                    ["Title"] = description.Name,
                    ["Name"] = $"var{i}{editor}",
                    ["Value"] = description.DefaultValue?.ToString()??string.Empty,
                    ["Editor"] = description.ParameterTypeName.Contains("SecureString") ? "password" : "string"
                };
                    
            }
            job.MessageQueue.PutMessage(new ShowMultiValuePromptMessage(options, "600", "200",
                string.IsNullOrEmpty(caption) ? "Sitecore PowerShell Extensions" : caption,
                string.IsNullOrEmpty(message) ? " " : message, string.Empty, string.Empty, string.Empty, false,
                null, null, host.SessionKey));
            var values = (object[]) job.MessageQueue.GetResult();

            return values?.Cast<Hashtable>()
                .ToDictionary(value => value["Name"].ToString(),
                    value =>
                        ((string) value["Name"]).Contains("password")
                            ? PSObject.AsPSObject(ToSecureString((string) value["Value"]))
                            : PSObject.AsPSObject(value["Value"]));
        }


        private static SecureString ToSecureString(string aString)
        {
            SecureString secure = null;
            if (!string.IsNullOrEmpty(aString))
            {
                secure = new SecureString();
                foreach (var c in aString)
                {
                    secure.AppendChar(c);
                }
            }
            return secure;
        }
        public override PSCredential PromptForCredential(string caption, string message, string userName,
            string targetName)
        {
            throw new NotImplementedException();
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName,
            string targetName, PSCredentialTypes allowedCredentialTypes,
            PSCredentialUIOptions options)
        {
            throw new NotImplementedException();
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices,
            int defaultChoice)
        {            
            if (!CheckSessionCanDoInteractiveAction(nameof(PromptForChoice))) return -1;


            var parameters =
                new Hashtable(choices.ToDictionary(p => "btn_" + choices.IndexOf(p),
                    p => WebUtil.SafeEncode(p.Label.Replace("&", ""))))
                {
                    {"te", message},
                    {"cp", caption},
                    {"dc", defaultChoice.ToString(CultureInfo.InvariantCulture)}
                };

            var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
            var job = jobManager.GetContextJob();

            Context.Site = Factory.GetSite(job.Options.SiteName);
            var lineWidth = choices.Count*80 + 140;
            var strLineWidth = lineWidth/8;
            var lineHeight = 0;
            foreach (var line in message.Split('\n'))
            {
                lineHeight += 1 + line.Length/strLineWidth;
            }
            lineHeight = Math.Max(lineHeight*21 + 130,150);
            var jobUiManager = TypeResolver.Resolve<IJobMessageManager>();
            var dialogResult = jobUiManager.ShowModalDialog(parameters, "ConfirmChoice",
                lineWidth.ToString(CultureInfo.InvariantCulture), lineHeight.ToString(CultureInfo.InvariantCulture));

            if (!string.IsNullOrEmpty(dialogResult))
            {
                return int.Parse(dialogResult.Substring(4));
            }
            return defaultChoice;
        }

        public Collection<int> PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, IEnumerable<int> defaultChoices)
        {
            Collection<int> results = new Collection<int>();
            int choice;
            do
            {
                choice = PromptForChoice(caption, message, choices, defaultChoices.FirstOrDefault());
                if (choice != -1)
                {
                    results.Add(choice);
                }
            } while (choice != -1);
            return results;
        }

        public virtual bool CheckSessionCanDoInteractiveAction(string operation, bool throwException = true)
        {
            if (!host.Interactive)
            {
                string message = string.IsNullOrEmpty(operation)
                    ? "Non interactive session cannot perform an interactive operation."
                    : $"Non interactive session cannot perform an interactive '{operation}' operation.";

                if (throwException)
                {
                    throw new InvalidOperationException(message);
                }
                PowerShellLog.Info(message);
            }
            return true;
        }

    }
}