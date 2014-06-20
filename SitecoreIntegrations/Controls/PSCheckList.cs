using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.PowerShell.Commands;
using Sitecore.Shell.Applications.ContentEditor;

namespace Cognifide.PowerShell.SitecoreIntegrations.Controls
{
    public class PSCheckList : Checklist
    {

        public void SetItemLanguage(string languageName)
        {
            ViewState["ItemLanguage"] = languageName;
        }
    }
}