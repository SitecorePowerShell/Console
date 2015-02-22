using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using Cognifide.PowerShell.Core.Settings;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Core.Host
{
    public class ScriptingHostUserInterface : PSHostUserInterface
    {
        private readonly ScriptingHostRawUserInterface rawUi;


        public ScriptingHostUserInterface(ApplicationSettings settings)
        {
            rawUi = new ScriptingHostRawUserInterface(settings);
        }

        /// <summary>
        ///     A reference to the PSHost implementation.
        /// </summary>
        public OutputBuffer Output
        {
            get { return rawUi.Output; }
        }

        public override PSHostRawUserInterface RawUI
        {
            get { return rawUi; }
        }

        public override string ReadLine()
        {
            throw new NotImplementedException();
        }

        public override SecureString ReadLineAsSecureString()
        {
            throw new NotImplementedException();
        }

        public override void Write(string value)
        {
            OutputLine lastline = Output[Output.Count - 1];
            if (!lastline.Terminated)
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
                ConsoleColor.Red,
                ConsoleColor.Black, true);
            Output.HasErrors = true;
            Output.AddRange(splitter);
        }

        public override void WriteDebugLine(string message)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Debug, "DEBUG: " + message, 
                RawUI.WindowSize.Width,
                ConsoleColor.Cyan, RawUI.BackgroundColor, true);
            Output.AddRange(splitter);
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            Message message = Message.Parse(this, "ise:updateprogress");
            message.Arguments.Add("Activity", record.Activity);
            message.Arguments.Add("ActivityId", record.ActivityId.ToString(CultureInfo.InvariantCulture));
            message.Arguments.Add("CurrentOperation", record.CurrentOperation);
            message.Arguments.Add("StatusDescription", record.StatusDescription);
            message.Arguments.Add("ParentActivityId", record.ParentActivityId.ToString(CultureInfo.InvariantCulture));
            message.Arguments.Add("PercentComplete", record.PercentComplete.ToString(CultureInfo.InvariantCulture));
            message.Arguments.Add("RecordType", record.RecordType.ToString());
            message.Arguments.Add("SecondsRemaining", record.SecondsRemaining.ToString(CultureInfo.InvariantCulture));
            var sheerMessage = new SendMessageMessage(message, false);
            if (JobContext.IsJob)
            {
                JobContext.MessageQueue.PutMessage(sheerMessage);
            }
            else
            {
                sheerMessage.Execute();
            }
        }

        public override void WriteVerboseLine(string message)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Verbose, message, RawUI, true);
            Output.AddRange(splitter);
        }

        public override void WriteWarningLine(string message)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Warning, message, RawUI.BufferSize.Width,
                ConsoleColor.Yellow, ConsoleColor.Black, true);
            Output.AddRange(splitter);
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message,
            Collection<FieldDescription> descriptions)
        {
            if (!Context.IsBackgroundThread)
            {
/*
                Sitecore.Context.ClientPage.ClientResponse.Input("Caption"
                                    , "*"
                                    , Sitecore.Configuration.Settings.ItemNameValidation
                                    , "'$Input' is not a valid name.", 255);
*/
                //args.WaitForPostBack();
            }
            throw new NotImplementedException();
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
            if (Context.Job == null)
            {
                throw new NotImplementedException();
            }

            var parameters =
                new Hashtable(choices.ToDictionary(p => "btn_" + choices.IndexOf(p),
                    p => WebUtil.SafeEncode(p.Label.Replace("&", ""))))
                {
                    {"te", message},
                    {"cp", caption},
                    {"dc", defaultChoice.ToString(CultureInfo.InvariantCulture)}
                };
            Context.Site = Factory.GetSite(Context.Job.Options.SiteName);
            int lineWidth = choices.Count*75 + 120;
            int strLineWidth = lineWidth/8;
            int lineHeight = 0;
            foreach (string line in message.Split('\n'))
            {
                lineHeight += 1 + line.Length/strLineWidth;
            }
            lineHeight = lineHeight*14 + 60;
            string dialogResult = JobContext.ShowModalDialog(parameters, "ConfirmChoice",
                lineWidth.ToString(CultureInfo.InvariantCulture), lineHeight.ToString(CultureInfo.InvariantCulture));

            if (!string.IsNullOrEmpty(dialogResult))
            {
                return int.Parse(dialogResult.Substring(4));
            }
            return -1;
        }
    }
}