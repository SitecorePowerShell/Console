using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Text.RegularExpressions;
using Sitecore.Shell.Applications.Dialogs;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    public static class CommandHelp
    {
        public static IEnumerable<string> GetHelp(ScriptSession session, string command)
        {
            Collection<PSParseError> errors;
            Collection<PSToken> tokens = PSParser.Tokenize(command, out errors);
            PSToken lastPsToken = tokens.Where(t => t.Type == PSTokenType.Command).LastOrDefault();
            if (lastPsToken != null)
            {
                session.Output.Clear();
                string lastToken = lastPsToken.Content;
                session.ExecuteScriptPart("Set-HostProperty -HostWidth 1000", false, true);
                session.ExecuteScriptPart(string.Format("Get-Help {0} -Full", lastToken), true, true);
                var sb = new StringBuilder();
                int headerCount = 0;
                int lineCount = 0;
                if (session.Output.Count == 0 || session.Output[0].LineType == OutputLineType.Error)
                {
                    return new string[] { "<div class='ps-help-command-name'>&nbsp;</div><div class='ps-help-header' align='center'>No Command in line or help information found</div><div class='ps-help-parameter' align='center'>Cannot provide help in this context.</div>" };
                }
                session.Output.ForEach(l =>
                {
                    if (!l.Text.StartsWith(" "))
                    {
                        headerCount++;
                    }
                    else
                    {
                        lineCount++;
                    }
                });
                bool listVIew = headerCount > lineCount;
                int lineNo = -1;
                session.Output.ForEach(l =>
                {
                    if (++lineNo > 0)
                    {
                        var line = System.Web.HttpUtility.HtmlEncode(l.Text.Trim());
                        line = Regex.Replace(line,
                            @"((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)",
                            "<a target='_blank' href='$1' class='ps-link'>$1</a>");
                        if (listVIew)
                        {
                            if (lineNo < 3)
                            {
                                sb.AppendFormat("<div class='ps-help-firstLine'>{0}</div>", line);
                            }
                            else
                            {
                                sb.AppendFormat("{0}\n", line);
                            }
                        }
                        else if (lineNo > 1)
                        {
                            if (lineNo == 2)
                            {
                                sb.AppendFormat("<div class='ps-help-command-name'>{0}</div>", line);
                            }
                            else if (!l.Text.StartsWith(" "))
                            {
                                sb.AppendFormat("<div class='ps-help-header'>{0}</div>", line);
                            }
                            else if (l.Text.StartsWith("    --")) // normal line with output or example
                            {
                                sb.AppendFormat("{0}\n", l.Text.Substring(4));
                            }
                            else if (l.Text.StartsWith("    -"))
                            {
                                sb.AppendFormat("<div class='ps-help-parameter'>{0}</div>", line);
                            }
                            else if (l.Text.StartsWith("    "))
                            {
                                line = (l.Text.StartsWith("    ") ? l.Text.Substring(4) : l.Text).Replace("<", "&lt;").Replace(">", "&gt;");
                                line = urlRegex.Replace(line, "<a href='$1' target='_blank'>$1</a>");
                                sb.AppendFormat("{0}\n", line);
                            }
                        }
                    }
                });
                session.Output.Clear();
                //IEnumerable<string> teResult = session.ExecuteScriptPart(string.Format("TabExpansion \"{0}\" \"{1}\"", command, lastToken),false,true).Cast<string>();
                //List<string> result = teResult.Select(l => truncatedCommand + l).ToList();
                var result = new string[] {sb.ToString()};
                return result;
            }
            return new string[] {"No Command in line found - cannot provide help in this context."};
        }

        static Regex urlRegex = new Regex(@"(?i)\b((?:[a-z][\w-]+:(?:/{1,3}|[a-z0-9%])|www\d{0,3}[.]|[a-z0-9.\-]+[.][a-z]{2,4}/)(?:[^\s()<>]+|\(([^\s()<>]+|(\([^\s()<>]+\)))*\))+(?:\(([^\s()<>]+|(\([^\s()<>]+\)))*\)|[^\s`!()\[\]{};:'"".,<>?«»“”‘’]))", RegexOptions.Singleline | RegexOptions.Compiled);
    }
}