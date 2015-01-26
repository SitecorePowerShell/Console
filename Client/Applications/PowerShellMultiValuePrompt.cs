using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI.WebControls;
using Cognifide.PowerShell.Client.Controls;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore;
using Sitecore.Controls;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Button = Sitecore.Web.UI.HtmlControls.Button;
using Checkbox = Sitecore.Web.UI.HtmlControls.Checkbox;
using Control = System.Web.UI.Control;
using DateTime = System.DateTime;
using ListItem = Sitecore.Web.UI.HtmlControls.ListItem;
using Literal = Sitecore.Web.UI.HtmlControls.Literal;
using Memo = Sitecore.Web.UI.HtmlControls.Memo;

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellMultiValuePrompt : DialogPage
    {
        private static readonly Regex typeRegex = new Regex(@".*clr(?<type>[\w]+)\s*",
            RegexOptions.Singleline | RegexOptions.Compiled);

        protected Literal Result;
        protected Literal DialogHeader;
        protected Literal DialogDescription;
        protected Literal TabOffsetValue;
        protected Border DataContextPanel;
        protected Scrollbox ValuePanel;
        protected Button OKButton;
        protected Button CancelButton;
        protected Tabstrip Tabstrip;
        protected bool ShowHints;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Tabstrip.OnTabClicked += tabstrip_OnChange;

            if (Sitecore.Context.ClientPage.IsEvent)
                return;

            HttpContext.Current.Response.AddHeader("X-UA-Compatible", "IE=edge");
            var sid = WebUtil.GetQueryString("sid");
            var variables = (object[]) HttpContext.Current.Session[sid];
            HttpContext.Current.Session.Remove(sid);

            var title = WebUtil.GetQueryString("te");
            ShowHints = WebUtil.GetQueryString("sh") == "1";

            if (!string.IsNullOrEmpty(title))
            {
                DialogHeader.Text = title;
            }

            var description = WebUtil.GetQueryString("ds");
            if (!string.IsNullOrEmpty(description))
            {
                DialogDescription.Text = description;
            }
            var okText = WebUtil.GetQueryString("ob");
            if (!string.IsNullOrEmpty(okText))
            {
                OKButton.Header = okText;
            }

            var cancelText = WebUtil.GetQueryString("cb");
            if (!string.IsNullOrEmpty(cancelText))
            {
                CancelButton.Header = cancelText;
            }

            AddControls(variables);
        }

        private void AddControls(object[] variables)
        {
            var tabs = new Dictionary<string, GridPanel>(StringComparer.OrdinalIgnoreCase);
            foreach (Hashtable variable in variables)
            {
                var tabName = variable["Tab"] as string;
                if (string.IsNullOrEmpty(tabName) || Tabstrip.Visible) continue;

                Tabstrip.Visible = true;
                break;
            }

            foreach (Hashtable variable in variables)
            {
                var tabName = (variable["Tab"] as string) ?? "Other";
                var name = variable["Title"] as string;
                var hint = (variable["Tip"] as string) ??
                           (variable["Hint"] as string) ?? (variable["Tooltip"] as string);
                var container = GetContainer(tabs, tabName);
                if (variable["Value"] == null || variable["Value"].GetType() != typeof (bool))
                {
                    var label = new Literal {Text = name, Class = "varTitle"};
                    container.Controls.Add(label);
                    if (ShowHints && !string.IsNullOrEmpty(hint))
                    {
                        label = new Literal {Text = hint, Class = "varHint"};
                        container.Controls.Add(label);
                    }
                }
                var input = GetVariableEditor(variable);
                container.Controls.Add(input);
            }

            TabOffsetValue.Text = string.Format("<script type='text/javascript'>var tabsOffset={0};</script>",
                tabs.Count > 0 ? 24 : 0);
        }

        public void tabstrip_OnChange(object sender, EventArgs e)
        {
            SheerResponse.Eval("ResizeDialogControls();");
        }

        private WebControl GetContainer(Dictionary<string, GridPanel> tabs, string tabName)
        {
            if (!Tabstrip.Visible)
            {
                return ValuePanel;
            }
            if (!tabs.ContainsKey(tabName))
            {
                var tab = new Tab {Header = tabName, ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("tab_")};
                //tab.Height = new Unit("100%");
                Tabstrip.Controls.Add(tab);
                Tabstrip.Width = new Unit("100%");
                //Tabstrip.Height = new Unit("100%");
                var panel = new GridPanel {Width = new Unit("100%"), CssClass = "ValuePanel"};
                tab.Controls.Add(panel);
                tabs.Add(tabName, panel);
                return panel;
            }
            return tabs[tabName];
        }

        private Control GetVariableEditor(Hashtable variable)
        {
            var value = variable["Value"];
            var name = (string) variable["Name"];
            var editor = variable["Editor"] as string;
            var type = value.GetType();

            if (type == typeof (DateTime) ||
                (!string.IsNullOrEmpty(editor) &&
                 (editor.IndexOf("date", StringComparison.OrdinalIgnoreCase) > -1 ||
                  editor.IndexOf("time", StringComparison.OrdinalIgnoreCase) > -1)))
            {
                var dateTimePicker = new DateTimePicker
                {
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                    ShowTime = (variable["ShowTime"] != null && (bool) variable["ShowTime"]) ||
                               (!string.IsNullOrEmpty(editor) &&
                                editor.IndexOf("time", StringComparison.OrdinalIgnoreCase) > -1)
                };
                if (value is DateTime)
                {
                    var date = (DateTime) value;
                    if (date != DateTime.MinValue && date != DateTime.MaxValue)
                    {
                        dateTimePicker.Value = (date.Kind == DateTimeKind.Utc)
                            ? DateUtil.ToIsoDate(DateUtil.ToServerTime(date))
                            : DateUtil.ToIsoDate(date);
                    }
                }
                else
                {
                    dateTimePicker.Value = (string) value ?? string.Empty;
                }
                return dateTimePicker;
            }

            if (!string.IsNullOrEmpty(editor) &&
                (editor.IndexOf("treelist", StringComparison.OrdinalIgnoreCase) > -1 ||
                 (editor.IndexOf("multilist", StringComparison.OrdinalIgnoreCase) > -1) ||
                 (editor.IndexOf("droplist", StringComparison.OrdinalIgnoreCase) > -1)))
            {
                Item item = null;
                List<Item> items = null;
                var strValue = string.Empty;
                if (value is Item)
                {
                    item = (Item) value;
                    items = new List<Item> {item};
                    strValue = item.ID.ToString();
                }
                else if (value is IEnumerable<object>)
                {
                    items = (value as IEnumerable<object>).Cast<Item>().ToList();
                    item = items.First();
                    strValue = string.Join("|", items.Select(i => i.ID.ToString()).ToArray());
                }

                var dbName = item == null ? Sitecore.Context.ContentDatabase.Name : item.Database.Name;
                if (editor.IndexOf("multilist", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    var multiList = new Multilist
                    {
                        ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                        Value = strValue,
                        Database = dbName,
                        ItemID = "{11111111-1111-1111-1111-111111111111}",
                        Source = variable["Source"] as string ?? "/sitecore"
                    };
                    multiList.Class += "  treePicker";
                    return multiList;
                }

                if (editor.IndexOf("droplist", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    var lookup = new LookupEx
                    {
                        ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                        Database = dbName,
                        ItemID = (item != null ? item.ID.ToString() : "{11111111-1111-1111-1111-111111111111}"),
                        Source = variable["Source"] as string ?? "/sitecore",
                        ItemLanguage = Sitecore.Context.Language.Name,
                        Value = (item != null ? item.ID.ToString() : "{11111111-1111-1111-1111-111111111111}")
                    };
                    lookup.Class += " textEdit";
                    return lookup;
                }
                var treeList = new TreeList
                {
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                    Value = strValue,
                    AllowMultipleSelection = true,
                    DatabaseName = dbName,
                    Database = dbName,
                    Source = variable["Source"] as string ?? "/sitecore",
                    DisplayFieldName = variable["DisplayFieldName"] as string ?? "__DisplayName"
                };
                treeList.Class += " treePicker";
                return treeList;
            }
            if (type == typeof (Item) ||
                (!string.IsNullOrEmpty(editor) && (editor.IndexOf("item", StringComparison.OrdinalIgnoreCase) > -1)))
            {
                var item = (Item) value;
                var source = variable["Source"] as string;
                var sourceDB = string.IsNullOrEmpty(source)
                    ? Sitecore.Context.ContentDatabase.Name
                    : StringUtil.ExtractParameter("databasename", source);
                var root = variable["Root"] as string;
                var sourceRoot = string.IsNullOrEmpty(source)
                    ? "/sitecore"
                    : StringUtil.ExtractParameter("DataSource", source);
                var dataContext = new DataContext
                {
                    DefaultItem = item.Paths.Path,
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("dataContext"),
                    Parameters = string.IsNullOrEmpty(source) ? "databasename=" + item.Database.Name : source,
                    DataViewName = "Master",
                    Root = string.IsNullOrEmpty(root) ? sourceRoot : root,
                    Database = item != null ? item.Database.Name : sourceDB,
                    Selected = new[] {new DataUri(item.ID, item.Language, item.Version)},
                    Folder = item.ID.ToString(),
                    Language = item.Language,
                    Version = item.Version
                };
                DataContextPanel.Controls.Add(dataContext);

                var treePicker = new TreePicker
                {
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                    Value = item.ID.ToString(),
                    DataContext = dataContext.ID
                };
/*
                if (source != null)
                {
                    string root = StringUtil.ExtractParameter("DataSource", source);
                    string db = StringUtil.ExtractParameter("DatabaseName", source);
                    dataContext.Root = string.IsNullOrEmpty(root) ? dataContext.Root : root;
                    dataContext.Database = string.IsNullOrEmpty(db) ? dataContext.Database : db;
                }
*/
                treePicker.Class += " treePicker";
                return treePicker;
            }

            if (type == typeof (bool) ||
                (!string.IsNullOrEmpty(editor) && (editor.IndexOf("bool", StringComparison.OrdinalIgnoreCase) > -1)))
            {
                var checkBox = new Checkbox
                {
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                    Header = (string) variable["Title"],
                    HeaderStyle = "display:inline-block;",
                    Checked = (bool) value
                };
                checkBox.Class = "varCheckbox";
                return checkBox;
            }

            if (!string.IsNullOrEmpty(editor))
            {
                var showRoles = editor.IndexOf("role", StringComparison.OrdinalIgnoreCase) > -1;
                var showUsers = editor.IndexOf("user", StringComparison.OrdinalIgnoreCase) > -1;
                var multiple = editor.IndexOf("multiple", StringComparison.OrdinalIgnoreCase) > -1;
                if (showRoles || showUsers)
                {
                    var picker = new UserPicker();
                    picker.Style.Add("float", "left");
                    picker.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_");
                    picker.Class += " scContentControl textEdit clr" + value.GetType().Name;
                    picker.Value = value.ToString();
                    picker.ExcludeRoles = !showRoles;
                    picker.ExcludeUsers = !showUsers;
                    picker.DomainName = variable["Domain"] as string ?? variable["DomainName"] as string;
                    picker.Multiple = multiple;
                    picker.Click = "UserPickerClick(" + picker.ID + ")";
                    return picker;
                }
            }

            Sitecore.Web.UI.HtmlControls.Control edit;
            if (variable["lines"] != null && ((int) variable["lines"] > 1))
            {
                edit = new Memo();
                edit.Attributes.Add("rows", variable["lines"].ToString());
            }
            else if (variable["Options"] != null)
            {
                var psOptions = variable["Options"].BaseObject();
                var options = new OrderedDictionary();
                if (psOptions is OrderedDictionary)
                {
                    options = psOptions as OrderedDictionary;
                }
                else if (psOptions is string)
                {
                    var strOptions = ((string) variable["Options"]).Split('|');
                    var i = 0;
                    while (i < strOptions.Length)
                    {
                        options.Add(strOptions[i++], strOptions[i++]);
                    }
                }
                else if (psOptions is Hashtable)
                {
                    var hashOptions = variable["Options"] as Hashtable;
                    foreach (var key in hashOptions.Keys)
                    {
                        options.Add(key, hashOptions[key]);
                    }
                }
                else
                {
                    throw new Exception("Checklist options format unrecognized.");
                }

                if (!string.IsNullOrEmpty(editor))
                {
                    if (editor.IndexOf("radio", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        var radioList = new Groupbox
                        {
                            ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                            Header = (string) variable["Title"],
                            Class = "scRadioGroup"
                        };

                        foreach (var option in options.Keys)
                        {
                            var optionName = option.ToString();
                            var optionValue = options[optionName].ToString();
                            var item = new Radiobutton
                            {
                                Header = optionName,
                                Value = optionValue,
                                ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID(radioList.ID),
                                Name = radioList.ID,
                                Checked = optionValue == value.ToString()
                            };
                            radioList.Controls.Add(item);
                            radioList.Controls.Add(new Literal("<br/>"));
                        }

                        return radioList;
                    }

                    if (editor.IndexOf("check", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        var checkList = new PSCheckList
                        {
                            ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                            //Header = (string)variable["Title"],                    
                            HeaderStyle = "margin-top:20px; display:inline-block;",
                            ItemID = "{11111111-1111-1111-1111-111111111111}"
                        };
                        checkList.SetItemLanguage(Sitecore.Context.Language.Name);
                        var values = new string[0];
                        if (value is string)
                        {
                            values = value.ToString().Split('|');
                        }
                        else if (value is IEnumerable)
                        {
                            var valueList = new List<string>();
                            foreach (var s in value as IEnumerable)
                            {
                                valueList.Add(s.ToString());
                            }
                            values = valueList.ToArray();
                        }
                        else
                        {
                            values = new[] {value.ToString()};
                        }
                        foreach (var option in options.Keys)
                        {
                            var optionName = option.ToString();
                            var optionValue = options[optionName].ToString();
                            var item = new ChecklistItem
                            {
                                ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID(checkList.ID),
                                Header = optionName,
                                Value = optionValue,
                                Checked = values.Contains(optionValue, StringComparer.OrdinalIgnoreCase)
                            };
                            checkList.Controls.Add(item);
                        }

                        checkList.TrackModified = false;
                        checkList.Disabled = false;
                        return checkList;
                    }
                }

                edit = new Combobox();
                foreach (var option in options.Keys)
                {
                    var optionName = option.ToString();
                    var optionValue = options[optionName].ToString();
                    var item = new ListItem
                    {
                        Header = optionName,
                        Value = optionValue
                    };
                    edit.Controls.Add(item);
                }
            }
            else
            {
                edit = new Edit();
            }
            if (!string.IsNullOrEmpty((string) variable["Tooltip"]))
            {
                edit.ToolTip = (string) variable["Tooltip"];
            }
            edit.Style.Add("float", "left");
            edit.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_");
            edit.Class += " scContentControl textEdit clr" + value.GetType().Name;
            edit.Value = value.ToString();

            return edit;
        }

        protected void UserPickerClick()
        {
            var requestParams = Sitecore.Context.ClientPage.Request.Params["__PARAMETERS"];
            if (requestParams.StartsWith("UserPickerClick("))
            {
                var controlId = requestParams.Substring(16, requestParams.IndexOf(')') - 16);
                var picker = ValuePanel.FindControl(controlId) as UserPicker;
                if (picker != null)
                {
                    Sitecore.Context.ClientPage.Start(picker, "Clicked");
                }
            }
        }

        protected void OKClick()
        {
            var variables = GetVariableValues();
            var sid = WebUtil.GetQueryString("sid");
            HttpContext.Current.Session[sid] = variables;
            SheerResponse.SetDialogValue(sid);
            SheerResponse.CloseWindow();
        }

        private object[] GetVariableValues()
        {
            var results = new List<object>();
            foreach (Sitecore.Web.UI.HtmlControls.Control control in ValuePanel.Controls)
            {
                if (control is Tabstrip)
                    foreach (WebControl tab in  control.Controls)
                        if (tab is Tab)
                            foreach (WebControl panel in tab.Controls)
                                if (panel is GridPanel)
                                    foreach (Sitecore.Web.UI.HtmlControls.Control editor in panel.Controls)
                                    {
                                        GetEditorValue(editor, results);
                                    }
                GetEditorValue(control, results);
            }
            return results.ToArray();
        }

        private void GetEditorValue(Sitecore.Web.UI.HtmlControls.Control control, List<object> results)
        {
            var controlId = control.ID;
            if (controlId != null && controlId.StartsWith("variable_"))
            {
                var result = GetVariableValue(control);
                results.Add(result);
            }
        }

        private object GetVariableValue(Sitecore.Web.UI.HtmlControls.Control control)
        {
            var controlId = control.ID;
            var parts = controlId.Split('_');

            var result = new Hashtable(2);
            result.Add("Name", String.Join("_", parts.Skip(1).Take(parts.Length - 2).ToArray()));

            var controlValue = control.Value;
            if (controlValue != null)
            {
                if (control is DateTimePicker)
                {
                    result.Add("Value", DateUtil.IsoDateToDateTime(controlValue));
                }
                else if (control is TreePicker)
                {
                    var contextID = (control as TreePicker).DataContext;
                    var context = (DataContext) DataContextPanel.FindControl(contextID);
                    result.Add("Value", context.CurrentItem);
                }
                else if (control is Checkbox)
                {
                    var boolValue = (control as Checkbox).Checked;
                    result.Add("Value", boolValue);
                }
                else if (control is Combobox)
                {
                    var boolValue = (control as Combobox).Value;
                    result.Add("Value", boolValue);
                }
                else if (control is TreeList)
                {
                    var treeList = control as TreeList;
                    var strIds = treeList.GetValue();
                    var ids = strIds.Split('|');
                    var db = Database.GetDatabase(treeList.DatabaseName);
                    var items = ids.Select(p => db.GetItem(p)).ToList();
                    result.Add("Value", items);
                }
                else if (control is Multilist)
                {
                    var multilist = control as Multilist;
                    var strIds = multilist.GetValue();
                    var ids = strIds.Split('|');
                    var items = ids.Select(p => Sitecore.Context.ContentDatabase.GetItem(p)).ToList();
                    result.Add("Value", items);
                }
                else if (control is LookupEx)
                {
                    var lookup = control as LookupEx;
                    result.Add("Value",
                        !string.IsNullOrEmpty(lookup.Value)
                            ? Sitecore.Context.ContentDatabase.GetItem(lookup.Value)
                            : null);
                }
                else if (control is PSCheckList)
                {
                    var checkList = control as PSCheckList;
                    var values =
                        checkList.Controls.Cast<Control>()
                            .Where(item => (item is ChecklistItem))
                            .Cast<ChecklistItem>()
                            .Where(checkItem => checkItem.Checked)
                            .Select(checkItem => checkItem.Value)
                            .ToArray();
                    result.Add("Value", values);
                }
                else if (control is Groupbox && control.Class.Contains("scRadioGroup"))
                {
                    foreach (var item in control.Controls)
                    {
                        if (item is Radiobutton)
                        {
                            var radioItem = item as Radiobutton;
                            if (radioItem.Checked)
                            {
                                result.Add("Value", radioItem.Value);
                                break;
                            }
                        }
                    }
                }
                else if (control is Edit || control is Memo)
                {
                    var value = control.Value;
                    var type = GetClrTypeName(control.Class);
                    switch (type)
                    {
                        case ("Int16"):
                            result.Add("Value", Int16.Parse(value));
                            break;
                        case ("Int32"):
                            result.Add("Value", Int32.Parse(value));
                            break;
                        case ("Int64"):
                            result.Add("Value", Int64.Parse(value));
                            break;
                        case ("UInt16"):
                            result.Add("Value", UInt16.Parse(value));
                            break;
                        case ("UInt32"):
                            result.Add("Value", UInt32.Parse(value));
                            break;
                        case ("UInt64"):
                            result.Add("Value", UInt64.Parse(value));
                            break;
                        case ("Byte"):
                            result.Add("Value", Byte.Parse(value));
                            break;
                        case ("Single"):
                            result.Add("Value", Single.Parse(value));
                            break;
                        case ("Double"):
                            result.Add("Value", Double.Parse(value));
                            break;
                        case ("Decimal"):
                            result.Add("Value", Decimal.Parse(value));
                            break;
                        default:
                            result.Add("Value", value);
                            break;
                    }
                }
                else if (control is UserPicker)
                {
                    result.Add("Value", control.Value.Split('|'));
                }
            }
            return result;
        }

        protected string GetClrTypeName(string classNames)
        {
            var typeMatch = typeRegex.Match(classNames);
            if (typeMatch.Success)
            {
                return typeMatch.Groups["type"].Value;
            }
            return string.Empty;
        }

        protected void CancelClick()
        {
            SheerResponse.CloseWindow();
        }
    }
}