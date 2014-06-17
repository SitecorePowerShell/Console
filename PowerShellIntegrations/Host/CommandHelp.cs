using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Sitecore.Data;
using Sitecore.Data.Items;

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
                session.SetVariable("helpFor", lastToken);
                Item scriptItem =
                    Database.GetDatabase("master")
                        .GetItem(ScriptLibrary.Path + "Internal/Context Help/Command Help");
                session.ExecuteScriptPart(scriptItem["script"], true, true);
                var sb = new StringBuilder("<div id=\"HelpClose\">x</div>");
                if (session.Output.Count == 0 || session.Output[0].LineType == OutputLineType.Error)
                {
                    return new[]
                    {
                        "<div class='ps-help-command-name'>&nbsp;</div><div class='ps-help-header' align='center'>No Command in line or help information found</div><div class='ps-help-parameter' align='center'>Cannot provide help in this context.</div>"
                    };
                }
                session.Output.ForEach(l => sb.Append(l.Text));
                session.Output.Clear();
                var result = new[] {sb.ToString()};
                return result;
            }
            return new[] {"No Command in line found - cannot provide help in this context."};
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