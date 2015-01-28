using System;
using System.Collections.Specialized;
using System.Web.UI;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Applications.ContentEditor.Galleries;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Text;
using Sitecore.Web.UI.WebControls.Ribbons;
using Control = Sitecore.Web.UI.HtmlControls.Control;

namespace Cognifide.PowerShell.Client.Controls
{
    public class IseContextPanel : RibbonPanel
    {
        public override void Render(HtmlTextWriter output, Ribbon ribbon, Item button, CommandContext context)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(ribbon, "ribbon");
            Assert.ArgumentNotNull(button, "button");
            Assert.ArgumentNotNull(context, "context");

            var contextDB = context.Parameters["contextDB"];
            var contextItemId = context.Parameters["contextItem"];
            var currentSessionId = context.Parameters["currentSessionId"];
            var currentSessionName = context.Parameters["currentSessionName"];
            var persistentSessionId = context.Parameters["persistentSessionId"];
            var contextItem = Factory.GetDatabase(contextDB).GetItem(contextItemId);

            output.Write("<div class=\"iseRibbonContextPanel\">");
            output.Write("<div class=\"scRibbonToolbarSmallButtons scRibbonContextLabels\">");
            output.Write("<div class=\"iseRibbonContextPanelLabel\">");
            output.Write(Translate.Text("Context"));
            output.Write("</div>");
            output.Write("<div class=\"iseRibbonContextPanelLabel\">");
            output.Write(Translate.Text("Session"));
            output.Write("</div>");
            output.Write("</div>");
            var contextEnabled = string.IsNullOrEmpty(persistentSessionId); 
            RenderContext(output, contextItem, ribbon, contextEnabled);
            var sessionButton = Factory.GetDatabase("core").GetItem("{0C784F54-2B46-4EE2-B0BA-72384125E123}");
            RenderSmallGalleryButton(output, sessionButton, context, ribbon, currentSessionName);
            output.Write("</div>");
        }

        private void RenderContext(HtmlTextWriter output, Item contextItem, Ribbon ribbon, bool contextEnabled)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(contextItem, "compareTo");
            Assert.ArgumentNotNull(ribbon, "ribbon");
            RenderSmallButton(output, ribbon, Control.GetUniqueID("export"),
                PathUtilities.GetItemPsPath(contextItem).EllipsisString(55),
                contextItem.Appearance.Icon, string.Empty,
                string.Format("ise:pickcontextitem(scriptDb={0},scriptID={1})", "",
                    ""), contextEnabled, false);
        }

        private void RenderSmallGalleryButton(HtmlTextWriter output, Item button, CommandContext commandContext,
            Ribbon ribbon, string title)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(button, "button");
            var enabled = CommandState.Enabled;
            var fieldValue = GetFieldValue(button, "Header");
            var icon = GetFieldValue(button, "Icon");
            var click = GetFieldValue(button, "Command");
            var str4 = GetFieldValue(button, "ID");
            var keyCode = GetFieldValue(button, "KeyCode");
            var str6 = GetFieldValue(button, "Access Key");
            var str7 = GetFieldValue(button, "Tooltip");
            if (click.Length > 0)
            {
                var command = CommandManager.GetCommand(GetClick(click));
                if (command != null)
                {
                    fieldValue = command.GetHeader(commandContext, fieldValue);
                    icon = command.GetIcon(commandContext, icon);
                    click = command.GetClick(commandContext, click);
                    enabled = CommandManager.QueryState(command, commandContext);
                }
            }
            if (enabled == CommandState.Hidden)
            {
                return;
            }
            var itemArray = (commandContext == null) ? new Item[0] : commandContext.Items;
            var item = (itemArray.Length > 0) ? itemArray[0] : null;
            UrlString itemUrl = null;
            itemUrl = GetItemUrl(item, (commandContext != null) ? commandContext.Parameters : null);
            var smallButton = new SmallGalleryButton
            {
                ID = "B" + button.ID.ToShortID()
            };
            itemUrl.Parameters["id"] = smallButton.ID;
            if (str4.Length > 0)
            {
                smallButton.ID = str4;
            }
            click = GetClick(click, commandContext);
            var width = GetFieldValue(button, "Gallery Width");
            var height = GetFieldValue(button, "Gallery Height");
            GalleryManager.GetGallerySize(smallButton.ID + "_frame", ref width, ref height);
            smallButton.Header = string.IsNullOrEmpty(title) ? fieldValue : title;
            smallButton.Icon = icon;
            smallButton.Command = click;
            smallButton.Gallery = GetFieldValue(button, "Gallery");
            smallButton.GalleryHeight = height;
            smallButton.GalleryWidth = width;
            if (itemUrl != null)
            {
                smallButton.GalleryUrl = itemUrl.ToString();
            }
            smallButton.Enabled = true;//enabled != CommandState.Disabled;
            smallButton.AccessKey = str6;
            smallButton.ToolTip = str7;
            smallButton.KeyCode = keyCode;
            ribbon.RenderSmallButton(output, smallButton);
            if (enabled != CommandState.Disabled)
            {
                Context.ClientPage.RegisterKey(keyCode, click, ribbon.ID);
            }
        }

        private static string GetClick(string click)
        {
            Assert.ArgumentNotNullOrEmpty(click, "click");
            var index = click.IndexOf('(');
            if (index >= 0)
            {
                return StringUtil.Left(click, index);
            }
            return click;
        }

        private static string GetFieldValue(Item item, string fieldName)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(fieldName, "fieldName");
            var id = TemplateManager.GetFieldId(fieldName, item.TemplateID, item.Database);
            if (ItemUtil.IsNull(id))
            {
                return string.Empty;
            }
            return (item.InnerData.Fields[id] ?? string.Empty);
        }

        private static UrlString GetItemUrl(Item item, NameValueCollection parameters)
        {
            var str = new UrlString();
            if (item != null)
            {
                str.Append("id", item.ID.ToString());
                str.Append("la", item.Language.ToString());
                str.Append("vs", item.Version.ToString());
                str.Append("db", item.Database.Name);
                str.Append("sc_content", item.Database.Name);
            }

            if (parameters != null)
            {
                foreach (string str2 in parameters)
                {
                    str.Append(str2, parameters[str2]);
                }
            }
            return str;
        }

        private static string GetClick(string click, CommandContext commandContext)
        {
            Assert.ArgumentNotNull(click, "click");
            var itemArray = (commandContext == null) ? new Item[0] : commandContext.Items;
            if (itemArray.Length == 1)
            {
                Assert.IsNotNull(commandContext, "context");
                click = click.Replace("$Target",
                    StringUtil.GetString(commandContext.Parameters["ControlID"]));
                click = click.Replace("$ItemID", itemArray[0].ID.ToString());
            }
            return click;
        }
    }
}