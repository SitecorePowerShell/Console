﻿using System.Collections.Specialized;
using System.Globalization;
using Sitecore;
using Sitecore.Shell.Applications.WebEdit.Commands;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Spe.Core.Extensions;

namespace Spe.Client.Commands
{
#pragma warning disable 612
    public class WebEditScriptCommand : WebEditCommand
#pragma warning restore 612
    {
        public override void Execute(CommandContext context)
        {
            var scriptId = context.Parameters["scriptId"];
            var scriptDb = context.Parameters["scriptDb"];

            var itemId = string.Empty;
            var itemDb = string.Empty;
            var itemLang = string.Empty;
            var itemVer = string.Empty;

            if (context.Items.Length > 0)
            {
                itemId = context.Items[0].ID.ToString();
                itemDb = context.Items[0].Database.Name;
                itemLang = context.Items[0].Language.Name;
                itemVer = context.Items[0].Version.Number.ToString(CultureInfo.InvariantCulture);
            }

            itemId =  context.Parameters.TryGetValue(NameValueCollectionExtensions.ItemId, itemId);
            itemDb = context.Parameters.TryGetValue(NameValueCollectionExtensions.ItemDb, itemDb);
            itemLang = context.Parameters.TryGetValue(NameValueCollectionExtensions.ItemLang, itemLang);
            itemVer = context.Parameters.TryGetValue(NameValueCollectionExtensions.ItemVer, itemVer);
            scriptId = context.Parameters.TryGetValue(NameValueCollectionExtensions.ScriptId, itemVer);
            
            var str = new UrlString(UIUtil.GetUri("control:PowerShellRunner"));
            str.Append(context.Parameters);
            str.Append("id", itemId);
            str.Append("db", itemDb);
            str.Append("lang", itemLang);
            str.Append("ver", itemVer);
            str.Append("scriptId", scriptId);
            str.Append("scriptDb", scriptDb);
            SheerResponse.ShowModalDialog(str.ToString(), "400", "260", "PowerShell Script Results", false);
        }

        private string TestParameter(NameValueCollection parameters, string[] keys, string fallback)
        {
            foreach (var key in keys)
            {
                if (!string.IsNullOrWhiteSpace(parameters[key]))
                {
                    return parameters[key];
                }
            }
            return fallback;
        }
    }
}
