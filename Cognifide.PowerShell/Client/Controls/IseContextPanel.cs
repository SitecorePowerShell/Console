using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web.UI;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Applications.ContentEditor.Galleries;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Text;
using Sitecore.Web.UI.WebControls.Ribbons;

namespace Cognifide.PowerShell.Client.Controls
{
    public class IseContextPanel : IseContextPanelBase
    {
        protected override Item Button1 => Factory.GetDatabase("core").GetItem("{C733DE04-FFA2-4DCB-8D18-18EB1CB898A3}");
        protected override Item Button2 => Factory.GetDatabase("core").GetItem("{0C784F54-2B46-4EE2-B0BA-72384125E123}");
        protected override string Label1 => ContextItem != null ? ContextItem.GetProviderPath().EllipsisString(50) : Texts.IseContextPanel_Render_none;
        protected override string Icon1 => ContextItem != null ? ContextItem.Appearance.Icon : Button1.Appearance.Icon;
        protected override string Label2 => CommandContext.Parameters["currentSessionName"];
        protected override string Icon2 => string.Empty;

        /*        public override void Render(HtmlTextWriter output, Ribbon ribbon, Item button, CommandContext context)
                {
                    Assert.ArgumentNotNull(output, "output");
                    Assert.ArgumentNotNull(ribbon, "ribbon");
                    Assert.ArgumentNotNull(button, "button");
                    Assert.ArgumentNotNull(context, "context");
                    var parameters = new NameValueCollection();

                    context.Parameters.AllKeys.Where(key => !key.StartsWith("$"))
                        .ForEach(key => parameters.Add(key, context.Parameters[key]));

                    var contextDb = context.Parameters["contextDB"];
                    var contextItemId = context.Parameters["contextItem"];
                    var currentSessionName = context.Parameters["currentSessionName"];
                    var contextItem = !string.IsNullOrEmpty(contextDb) && !string.IsNullOrEmpty(contextItemId)
                        ? Factory.GetDatabase(contextDb).GetItem(contextItemId)
                        : null;

                    output.Write("<div class=\"iseRibbonContextPanel {0}\">",
                        context.Parameters["ScriptRunning"] == "1" ? "disabled" : string.Empty);
                    // timestamp added because Sitecore won't re-send it to browser it if content didn't change - so changing session to the same wouldn't close the dropdowns
                    output.Write("<div class=\"scRibbonToolbarSmallButtons scRibbonContextLabels\" timestamp=\"{0}\">",
                        DateTime.Now.ToString("O"));
                    output.Write("<div class=\"iseRibbonContextPanelLabel\">");
                    output.Write(Translate.Text(Texts.IseContextPanel_Render_Context));
                    output.Write("</div>");
                    output.Write("<div class=\"iseRibbonContextPanelLabel\">");
                    output.Write(Translate.Text(Texts.IseContextPanel_Render_Session));
                    output.Write("</div>");
                    output.Write("</div>");
                    var contextButton = Factory.GetDatabase("core").GetItem("{C733DE04-FFA2-4DCB-8D18-18EB1CB898A3}");
                    RenderSmallGalleryButton(output, contextButton, context, ribbon, path, icon, parameters);
                    var sessionButton = Factory.GetDatabase("core").GetItem("{0C784F54-2B46-4EE2-B0BA-72384125E123}");
                    RenderSmallGalleryButton(output, sessionButton, context, ribbon, currentSessionName, string.Empty, parameters);
                    output.Write("</div>");
                }

                private static void RenderSmallGalleryButton(HtmlTextWriter output, Item button, CommandContext commandContext,
                    Ribbon ribbon, string title, string overrideIcon, NameValueCollection parameters)
                {
                    Assert.ArgumentNotNull(output, "output");
                    Assert.ArgumentNotNull(button, "button");
                    var enabled = CommandState.Enabled;
                    var fieldValue = GetFieldValue(button, "Header");
                    var icon = string.IsNullOrEmpty(overrideIcon) ? GetFieldValue(button, "Icon") : overrideIcon;
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
                    var itemUrl = GetItemUrl(item, (commandContext != null) ? commandContext.Parameters : null);
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
                    smallButton.GalleryUrl = itemUrl.ToString();
                    smallButton.Enabled = enabled != CommandState.Disabled;
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
                    return index >= 0 ? StringUtil.Left(click, index) : click;
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
                    Assert.ArgumentNotNull(commandContext, "commandContext");
                    var itemArray = (commandContext == null) ? new Item[0] : commandContext.Items;
                    if (itemArray.Length == 1)
                    {
                        Assert.IsNotNull(commandContext, "context");
                        click = click.Replace("$Target",
                            StringUtil.GetString(commandContext.Parameters["ControlID"]));
                        click = click.Replace("$ItemID", itemArray[0].ID.ToString());
                    }
                    return click;
                }*/
    }
}