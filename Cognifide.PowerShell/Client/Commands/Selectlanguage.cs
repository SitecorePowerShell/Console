using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Commands
{
    [Serializable]
    public class SelectLanguage : Command
    {
        // Methods
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            Context.ClientPage.Start(this, "Run", context.Parameters);
        }

        protected void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack)
            {
                if (args.HasResult)
                {
                    Context.ClientPage.SendMessage(this, $"ise:setlanguage(language={args.Result})");
                }
            }
            else
            {
                //SheerResponse.CheckModified(false);
                UrlString str =
                    new UrlString("/sitecore/shell/~/xaml/Sitecore.Shell.Applications.Globalization.SelectLanguage.aspx")
                    {
                        ["la"] = args.Parameters["currLang"]
                    };
                SheerResponse.ShowModalDialog(str.ToString(), true);
                args.WaitForPostBack();
            }
        }
    }


}