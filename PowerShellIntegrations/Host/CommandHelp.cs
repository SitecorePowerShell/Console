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
            string lastToken = tokens.Last().Content;
            string truncatedCommand = string.Empty;
            switch (tokens.Count)
            {
                case (0):
                    break;
                case (1):
                    break;
                default:

                    truncatedCommand = tokens.Take(tokens.Count - 1).
                                              Select(
                                                  l =>
                                                  l.Content.Contains(" ")
                                                      ? string.Format("\"{0}\"", l.Content)
                                                      : l.Content)
                                             .
                                              Aggregate((x, y) => x + " " + y) + " ";
                    break;
            }
            lastToken = lastToken.Trim('"', '\'');
            session.ExecuteScriptPart("Set-HostProperty -HostWidth 1000", false, true);
            session.ExecuteScriptPart(string.Format("Get-Help {0} -Full", lastToken), true, true);
            StringBuilder sb = new StringBuilder();
            int headerCount = 0;
            int lineCount = 0;
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
                        else if (l.Text.StartsWith("    -"))
                        {
                            sb.AppendFormat("<div class='ps-help-parameter'>{0}</div>", line);
                        }
                        else
                        {
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
    }
}