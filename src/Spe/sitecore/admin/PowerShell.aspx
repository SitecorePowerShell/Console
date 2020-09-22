<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PowerShell.aspx.cs" Inherits="Spe.Client.Applications.Administration.PowerShell" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>PowerShell No Frills ISE</title>
    <link rel="Stylesheet" type="text/css" href="/sitecore/shell/themes/standard/default/WebFramework.css" />
    <link rel="Stylesheet" type="text/css" href="/sitecore modules/PowerShell/Styles/Dialogs.css" runat="server" />
    <link rel="Stylesheet" type="text/css" href="/sitecore modules/PowerShell/Styles/Console.css" runat="server" />
    <style>
        .error {
            font-style: italic;
            color: red;
        }

        .content textarea {
            font-family: "Cascadia Code", Monaco, Menlo, "Ubuntu Mono", source-code-pro, monospace;
        }

        .console pre {
            margin: 0 10px;
            font-family: "Cascadia Code", Monaco, Menlo, "Ubuntu Mono", source-code-pro, monospace;
            font-size: 20px;
        }

        #ScriptResult pre {
            position: initial;
        }

        .top-half, .bottom-half {
            left: 0;
            right: 0;
            position: fixed;
            text-align: left;
        }

        .top-half {
            top: 0;
            width: 900px;
            margin: 0 auto;
            margin-bottom: 24px;
            background: white;
            overflow: auto;
            height: 40%;
        }

        .top-half .content {
            padding: 0 10px;
        }

        .bottom-half {
            bottom: 0;
            overflow: auto;
            background-color: #012456;
            width: 100%;
            height: 60%;
        }
    </style>
</head>
<body>
    <form id="Form1" runat="server">
        <div class="top-half">
            <div class="content">
                <h1>
                    <a href="/sitecore/admin/">Administration Tools</a> - PowerShell
                </h1>
                <asp:PlaceHolder ID="ErrorMessage" runat="server">
                    <p>&nbsp;</p>
                </asp:PlaceHolder>
                <asp:TextBox TextMode="MultiLine" Rows="15" Columns="100" runat="server" ID="Query" placeholder="Get-Item -Path 'master:'" />
                <br />
                <asp:Button ID="Button1" runat="server" OnClick="Execute" Text="Execute" />
                <div>&nbsp;</div>

            </div>
        </div>
        <div runat="server" id="Output" enableviewstate="False" class="console bottom-half"></div>
    </form>
</body>
</html>
