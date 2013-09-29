using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    public static class CommandHelp
    {
        public static IEnumerable<string> GetHelpOld(ScriptSession session, string command)
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
                    return new string[]
                    {
                        "<div class='ps-help-command-name'>&nbsp;</div><div class='ps-help-header' align='center'>No Command in line or help information found</div><div class='ps-help-parameter' align='center'>Cannot provide help in this context.</div>"
                    };
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
                                line =
                                    (l.Text.StartsWith("    ") ? l.Text.Substring(4) : l.Text).Replace("<", "&lt;")
                                        .Replace(">", "&gt;");
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

        private static Regex urlRegex =
            new Regex(
                @"(?i)\b((?:[a-z][\w-]+:(?:/{1,3}|[a-z0-9%])|www\d{0,3}[.]|[a-z0-9.\-]+[.][a-z]{2,4}/)(?:[^\s()<>]+|\(([^\s()<>]+|(\([^\s()<>]+\)))*\))+(?:\(([^\s()<>]+|(\([^\s()<>]+\)))*\)|[^\s`!()\[\]{};:'"".,<>?«»“”‘’]))",
                RegexOptions.Singleline | RegexOptions.Compiled);

        public static IEnumerable<string> GetHelp(ScriptSession session, string command)
        {
            Collection<PSParseError> errors;
            Collection<PSToken> tokens = PSParser.Tokenize(command, out errors);
            PSToken lastPsToken = tokens.Where(t => t.Type == PSTokenType.Command).LastOrDefault();
            //if (lastPsToken != null)
            {
                session.Output.Clear();
                string lastToken = lastPsToken.Content;
                session.SetVariable("helpFor",lastToken);
                Item scriptItem =
                    Database.GetDatabase("master")
                        .GetItem("/sitecore/system/Modules/PowerShell/Script Library/Internal/Context Help/Command Help");
                session.ExecuteScriptPart(scriptItem["script"], true, true);
                var sb = new StringBuilder();
                if (session.Output.Count == 0 || session.Output[0].LineType == OutputLineType.Error)
                {
                    return new string[]
                    {
                        "<div class='ps-help-command-name'>&nbsp;</div><div class='ps-help-header' align='center'>No Command in line or help information found</div><div class='ps-help-parameter' align='center'>Cannot provide help in this context.</div>"
                    };
                }
                session.Output.ForEach(l => sb.Append(l.Text));
                session.Output.Clear();
                var result = new string[] {sb.ToString()};
                return result;
            }
            return new string[] {"No Command in line found - cannot provide help in this context."};
        }

        private const string helpScript = @"#Based on http://poshcode.org/1612
function FixString {
  param($in = '')
  if ($in -eq $null) {
    $in = ''
  }
  return $in.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;').Replace(""
"",""<br/>
"")
}

$c = get-help $helpFor -full

""
    <h1 class='title'>$($c.Name)</h1>
    <div>$($c.synopsis)</div>
    
    <h2> Syntax </h2>
    <div class='codeSnippetContainerCodeContainer'><div class='codeSnippetContainerCode'>
    <code>$(FixString($c.syntax | out-string  -width 2000).Trim())</code>
    </div></div> 
 
    <h2> Detailed Description </h2>
    <div>$(FixString($c.Description  | out-string  -width 2000))</div>
 
    <h2> Related Commands </h2>
    <div>
""
foreach ($relatedLink in $c.relatedLinks.navigationLink) {
    if($relatedLink.linkText -ne $null -and $relatedLink.linkText.StartsWith('about') -eq $false)
    {
        if($relatedLink.uri -eq $null -or $relatedLink.uri -eq ''){
           if($relatedLink.linkText -match 'http'){
             $url = $relatedLink.linkText | select-string -pattern '\b(?:(?:https?|ftp|file)://|www\.|ftp\.)(?:\([-A-Z0-9+&@#/%=~_|$?!:,.]*\)|[-A-Z0-9+&@#/%=~_|$?!:,.])*(?:\([-A-Z0-9+&@#/%=~_|$?!:,.]*\)|[A-Z0-9+&@#/%=~_|$])' | % { $_.Matches } | % { $_.Value } | select -first 1
             ""    * <a href='$url' target='_blank'>$($relatedLink.linkText)</a><br/>""
           } else {
             $internalCommand = '""'+$relatedLink.linkText+'""'
             ""    * <a href='#' onclick='javascript:return cognifide.powershell.showCommandHelp($internalCommand);'>$($relatedLink.linkText)</a><br/>""
           }
        } else {
            ""    * <a href='$($relatedLink.uri)' target='_blank'>$($relatedLink.linkText) $($relatedLink.uri)</a><br/>""
        }
    }
}

""   </div>
    <h2> Parameters </h2>
    <table border='1'>
        <tr>
            <th>Name</th>
            <th>Description</th>
            <th>Required?</th>
            <th>Pipeline Input</th>
            <th>Default Value</th>
        </tr>
""
$paramNum = 0

foreach ($param in $c.parameters.parameter ) 
{
    ""
        <tr valign='top'>
            <td>$($param.Name)&nbsp;</td>
            <td>$(FixString(($param.Description  | out-string  -width 2000).Trim()))&nbsp;</td>
            <td>$(FixString($param.Required))&nbsp;</td>
            <td>$(FixString($param.PipelineInput))&nbsp;</td>
            <td>$(FixString($param.DefaultValue))&nbsp;</td>
        </tr>
    ""
}
""   </table>""
   
# Input Type
if (($c.inputTypes | Out-String ).Trim().Length -gt 0) 
{
    ""
    <h2> Input Type </h2>
    <div>$(FixString($c.inputTypes  | out-string  -width 2000).Trim())</div>
    ""
}
   
# Return Type
if (($c.returnValues | Out-String ).Trim().Length -gt 0) 
{
    ""
    <h2> Return Values </h2>
    <div>$(FixString($c.returnValues  | out-string  -width 2000).Trim())</div>
    ""
}
         
# Notes
if (($c.alertSet | Out-String).Trim().Length -gt 0) 
{
    ""
    <h2> Notes </h2>
    <div>$(FixString($c.alertSet | out-string -Width 2000).Trim())</div>
    ""
}
 
# Examples
if (($c.examples | Out-String).Trim().Length -gt 0) 
{
    ""
    <h2> Examples </h2>""
    foreach ($example in $c.examples.example) 
    {
        ""
        <h3> $(FixString($example.title.Trim(('-',' '))))</h3>
        <div class='codeSnippetContainerCodeContainer'><div class='codeSnippetContainerCode'>
        <pre>$(FixString($example.code | out-string ).Trim())</pre>
        </div></div>
        $example.remarks | fl
        <div>$(FixString($example.remarks | out-string -Width 2000).Trim())</div>
        ""
    }
}
";
    }
}