using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    public class ScriptingHostUserInterface : PSHostUserInterface
    {
        private readonly ScriptingHostRawUserInterface rawUi;

        public ScriptingHostUserInterface(ApplicationSettings settings)
        {
            Output = new OutputBuffer();
            rawUi = new ScriptingHostRawUserInterface(settings);
        }

        /// <summary>
        ///     A reference to the PSHost implementation.
        /// </summary>
        public OutputBuffer Output { get; private set; }

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
            var splitter = new BufferSplitterCollection(OutputLineType.Output, value, RawUI);
            OutputLine lastline = Output[Output.Count - 1];
            if (lastline.LineType == OutputLineType.Output && lastline.ForegroundColor == rawUi.ForegroundColor &&
                lastline.BackgroundColor == RawUI.BackgroundColor)
            {
                lastline.Text += value;
            }
            else
            {
                Output.AddRange(splitter);
            }
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Output, value, RawUI.BufferSize.Width, foregroundColor,
                                              backgroundColor);
            Output.AddRange(splitter);
        }

        public override void WriteLine(string value)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Output, value, RawUI);
            Output.AddRange(splitter);
        }

        public override void WriteErrorLine(string value)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Error, value, RawUI.BufferSize.Width, ConsoleColor.Red,
                                              ConsoleColor.Black);
            Output.AddRange(splitter);
        }

        public override void WriteDebugLine(string message)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Debug, message, RawUI);
            Output.AddRange(splitter);
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Progress, record.StatusDescription, RawUI);
            Output.AddRange(splitter);
        }

        public override void WriteVerboseLine(string message)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Verbose, message, RawUI);
            Output.AddRange(splitter);
        }

        public override void WriteWarningLine(string message)
        {
            var splitter = new BufferSplitterCollection(OutputLineType.Warning, message, RawUI.BufferSize.Width,
                                              ConsoleColor.Yellow, ConsoleColor.Black);
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

            string dialogResult = JobContext.ShowModalDialog(parameters, "ConfirmChoice", "800", "300");
            if (!string.IsNullOrEmpty(dialogResult))
            {
                return int.Parse(dialogResult.Substring(4));
            }
            return -1;
        }
    }
}