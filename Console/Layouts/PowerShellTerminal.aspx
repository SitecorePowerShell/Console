<%@ Page Language="C#" CodeBehind="PowerShellTerminal.aspx.cs" Inherits="Cognifide.PowerShell.Console.Layouts.PowerShellTerminal" %>
<%@ Import Namespace="Cognifide.PowerShell.PowerShellIntegrations.Settings" %>
<!DOCTYPE html>
<!-- !DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd" -->
<html>
    <head runat="server">
        <meta name="robots" content="noindex, nofollow" />
        <!-- meta http-equiv="X-UA-Compatible" content="IE=EmulateIE7" / -->
        <link href="~/sitecore/shell/themes/standard/default.css" type="text/css" rel="stylesheet" />

        <link href="~/Console/Styles/Console.css" type="text/css" rel="stylesheet" />
        <title>PowerShell for Sitecore</title>
        <style>
            .terminal, .terminal .terminal-output,
            .terminal .terminal-output div,
            .terminal .terminal-output div div,
            .cmd, .terminal .cmd span, .terminal .cmd div {
                color: <%= ForegroundColor %>;
                background-color: <%= BackgroundColor %>;
            }
        </style>
        <script type="text/javascript" src="/Console/Scripts/jquery-1.10.2.min.js"> </script>
        <script type="text/javascript" src="/Console/Scripts/jquery.mousewheel-min.js"> </script>
        <script type="text/javascript" src="/Console/Scripts/jquery.terminal.js"> </script>
        <link href="/Console/Styles/jquery.terminal.css" type="text/css" rel="Stylesheet" />
        <script type="text/javascript" src="/Console/Scripts/split.js"> </script>
        <!--[if lt IE 9]>
            <script type="text/javascript" language="javascript" src="/Console/Scripts/json2.js"> </script>
        <![endif]-->

        <script type="text/javascript" src="/Console/Scripts/console.js"> </script>
        <script>
            $(function() {
                cognifide.powershell.setOptions({
                    initialPoll: <%= WebServiceSettings.InitialPollMillis %>,
                    maxPoll: <%= WebServiceSettings.MaxmimumPollMillis %>
                });
            });
        </script>
    </head>
    <body style="overflow: visible;">
        <a href="#" class="tip_no_hints">No hints found</a>
        <div id="terminal"></div>
        <div id="working">
            <img src="/Console/Assets/working.gif" alt="Working">
        </div>
    </body>
</html>