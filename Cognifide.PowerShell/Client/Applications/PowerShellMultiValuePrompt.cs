using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Cognifide.PowerShell.Client.Controls;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Cognifide.PowerShell.Core.VersionDecoupling.Interfaces;
using Sitecore;
using Sitecore.Controls;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Shell.Applications.Dialogs.RulesEditor;
using Sitecore.Shell.Applications.Rules;
using Sitecore.StringExtensions;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
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
        private static readonly Regex TypeRegex = new Regex(@".*clr(?<type>[\w]+)\s*",
            RegexOptions.Singleline | RegexOptions.Compiled);

        protected Button CancelButton;
        protected Border DataContextPanel;
        protected Literal DialogDescription;
        protected Literal DialogHeader;
        protected Literal CustomStyles;
        protected Button OKButton;
        protected Literal Result;
        protected bool ShowHints;
        protected Literal TabOffsetValue;
        protected Tabstrip Tabstrip;
        protected Scrollbox ValuePanel;

        public static string MandatoryVariables
        {
            get { return StringUtil.GetString(Sitecore.Context.ClientPage.ServerProperties["MandatoryVariables"]); }
            set { Sitecore.Context.ClientPage.ServerProperties["MandatoryVariables"] = value; }
        }

        public static string Validator
        {
            get { return StringUtil.GetString(Sitecore.Context.ClientPage.ServerProperties["Validator"]); }
            set { Sitecore.Context.ClientPage.ServerProperties["Validator"] = value; }
        }

        public static Dictionary<string, string> FieldValidators
        {
            get
            {
                var fv = (Dictionary<string, string>) Sitecore.Context.ClientPage.ServerProperties["FieldValidators"];
                if (fv == null)
                {
                    fv = new Dictionary<string, string>();
                    Sitecore.Context.ClientPage.ServerProperties["FieldValidators"] = fv;
                }
                return fv;
            }
            set { Sitecore.Context.ClientPage.ServerProperties["FieldValidators"] = value; }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Tabstrip.OnTabClicked += tabstrip_OnChange;

            if (Sitecore.Context.ClientPage.IsEvent)
                return;

            HttpContext.Current.Response.AddHeader("X-UA-Compatible", "IE=edge");
            var sid = WebUtil.GetQueryString("sid");
            var message = (ShowMultiValuePromptMessage) HttpContext.Current.Cache[sid];
            var variables = message.Parameters;
            HttpContext.Current.Cache.Remove(sid);

            var title = message.Title;
            ShowHints = message.ShowHints;

            if (!string.IsNullOrEmpty(title))
            {
                DialogHeader.Text = title;
            }

            var description = message.Description;
            if (!string.IsNullOrEmpty(description))
            {
                DialogDescription.Text = description;
            }
            var okText = message.OkButtonName;
            if (!string.IsNullOrEmpty(okText))
            {
                OKButton.Header = okText;
            }

            var cancelText = message.CancelButtonName;
            if (!string.IsNullOrEmpty(cancelText))
            {
                CancelButton.Header = cancelText;
            }

            Validator = message.Validator?.ToString();

            //MandatoryVariables =
            var mandatoryVariables =
                variables.Cast<Hashtable>()
                    .Where(p => p["Mandatory"] is bool && (bool) p["Mandatory"])
                    .Select(v => v["Name"]).ToList();
            if (mandatoryVariables.Any())
            {
                MandatoryVariables = (string) mandatoryVariables.Aggregate((accumulated, next) =>
                        next + "," + accumulated);
            }
            AddControls(variables);
            SitecoreVersion.V82.OrNewer(() =>
                        CustomStyles.Text = "<style>.scContentButton { color: rgb(38, 148, 192);}</style>"
            );
        }

        private void AddControls(object[] variables)
        {
            var tabs = new Dictionary<string, Border>(StringComparer.OrdinalIgnoreCase);
            Tabstrip.Visible =
                variables.Cast<Hashtable>()
                    .Select(variable => variable["Tab"] as string)
                    .Any(tabName => !string.IsNullOrEmpty(tabName));
            var fieldValidators = FieldValidators;
            foreach (Hashtable variable in variables)
            {
                var tabName = variable["Tab"] as string ?? "Other";
                var name = variable["Name"] as string ?? string.Empty;
                var title = variable["Title"] as string ?? name;
                var hint = variable["Tip"] as string ??
                           variable["Hint"] as string ?? variable["Tooltip"] as string;
                var variableValue = variable["Value"];
                var columns = 12;
                var clearfix = false;
                var height = variable["Height"] as string;
                var validator = variable["Validator"] as ScriptBlock;

                if (validator != null)
                {
                    fieldValidators.Add(name, validator.ToString());
                }

                if (variable["Columns"] != null)
                {
                    var strColumns = variable["Columns"].ToString().Split(' ');
                    foreach (var columnValue in strColumns)
                    {
                        if (int.TryParse(columnValue, out columns))
                        {
                            if (columns > 12 && columns < 0)
                            {
                                columns = 12;
                            }
                        }
                        break;
                    }
                    clearfix = strColumns.Contains("first", StringComparer.OrdinalIgnoreCase);
                }

                // get the control in which the variable should be placed.
                var container = GetContainer(tabs, tabName);

                // retrieve the variable editor control.
                Control variableEditor;
                try
                {
                    variableEditor = GetVariableEditor(variable);
                }
                catch (Exception ex)
                {
                    PowerShellLog.Error($"Error while rendering editor in Read-Variable cmdlet for variable {title}", ex);
                    variableEditor = new Literal
                    {
                        Text = Texts.PowerShellMultiValuePrompt_AddControls_Error_while_rendering_this_editor,
                        Class = "varHint"
                    };
                }

                // create and set up the variable wrapper
                var variableWrapper = new Border
                {
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_")
                };

                // add variable title and tooltip if it's not a checkbox
                if (variableValue == null || variableValue.GetType() != typeof(bool))
                {
                    var label = new Literal {Text = title, Class = "varTitle"};
                    variableWrapper.Controls.Add(label);
                    if (ShowHints && !string.IsNullOrEmpty(hint))
                    {
                        label = new Literal {Text = hint, Class = "varHint"};
                        variableWrapper.Controls.Add(label);
                    }
                }

                if (!string.IsNullOrEmpty(height))
                {
                    variableWrapper.Height = new Unit(height);
                    variableWrapper.Class = "variableWrapper variableWrapperFixedHeight";
                }
                else
                {
                    variableWrapper.Class = "variableWrapper";
                }

                variableWrapper.Class += " grid-" + columns;
                if (clearfix)
                {
                    variableWrapper.Class += " clearfix";
                }

                // add editor to the wrapper
                variableWrapper.Controls.Add(variableEditor);

                // add validator indicator
                var variableValidator = new Border
                {
                    ID = $"var_{name}_validator",
                    InnerHtml = "",
                    Class = "validator"
                };
                variableWrapper.Controls.Add(variableValidator);

                // add wrapper to the container
                container.Controls.Add(variableWrapper);
            }

            FieldValidators = fieldValidators;
            TabOffsetValue.Text = $"<script type='text/javascript'>var tabsOffset={(tabs.Count > 0 ? 24 : 0)};</script>";
        }

        public void tabstrip_OnChange(object sender, EventArgs e)
        {
            SitecoreVersion.V81.OrOlder(() =>
                        SheerResponse.Eval("ResizeDialogControls();")
            );
        }

        private WebControl GetContainer(IDictionary<string, Border> tabs, string tabName)
        {
            if (!Tabstrip.Visible)
            {
                return ValuePanel;
            }

            if (tabs.ContainsKey(tabName)) return tabs[tabName];

            var tab = new Tab {Header = tabName, ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("tab_")};
            Tabstrip.Controls.Add(tab);
            Tabstrip.Width = new Unit("100%");
            var border = new Border();
            tab.Controls.Add(border);
            tabs.Add(tabName, border);
            return border;
        }

        private Control GetVariableEditor(IDictionary variable)
        {
            var value = variable["Value"];
            var name = (string) variable["Name"];
            var editor = variable["Editor"] as string;
            var type = value.GetType();

            if (type == typeof(DateTime) ||
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
                        dateTimePicker.Value = date.Kind != DateTimeKind.Utc
                            ? DateUtil.ToIsoDate(TypeResolver.Resolve<IDateConverter>().ToServerTime(date))
                            : DateUtil.ToIsoDate(date);
                    }
                }
                else
                {
                    dateTimePicker.Value = value as string ?? string.Empty;
                }
                return dateTimePicker;
            }

            if (!string.IsNullOrEmpty(editor) && editor.IndexOf("rule", StringComparison.OrdinalIgnoreCase) > -1)
            {
                var editorId = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_");
                Sitecore.Context.ClientPage.ServerProperties[editorId] = value;

                var rulesBorder = new Border
                {
                    Class = "rulesWrapper",
                    ID = editorId
                };

                var rulesEditButton = new Button
                {
                    Header = Texts.PowerShellMultiValuePrompt_GetVariableEditor_Edit_rule,
                    Class = "scButton edit-button rules-edit-button",
                    Click = "EditConditionClick(\\\"" + editorId + "\\\")"
                };

                rulesBorder.Controls.Add(rulesEditButton);
                var rulesRender = new Literal
                {
                    ID = editorId + "_renderer",
                    Text = GetRuleConditionsHtml(
                        string.IsNullOrEmpty(value as string) ? "<ruleset />" : value as string)
                };
                rulesRender.Class = rulesRender.Class + " varRule";
                rulesBorder.Controls.Add(rulesRender);
                return rulesBorder;
            }

            if (!string.IsNullOrEmpty(editor) &&
                (editor.IndexOf("treelist", StringComparison.OrdinalIgnoreCase) > -1 ||
                 (editor.IndexOf("multilist", StringComparison.OrdinalIgnoreCase) > -1) ||
                 (editor.IndexOf("droplist", StringComparison.OrdinalIgnoreCase) > -1) ||
                 (editor.IndexOf("droptree", StringComparison.OrdinalIgnoreCase) > -1)))
            {
                Item item = null;
                var strValue = string.Empty;
                if (value is Item)
                {
                    item = (Item) value;
                    strValue = item.ID.ToString();
                }
                else if (value is IEnumerable<object>)
                {
                    var items = (value as IEnumerable<object>).Cast<Item>().ToList();
                    item = items.FirstOrDefault();
                    strValue = string.Join("|", items.Select(i => i.ID.ToString()).ToArray());
                }

                var dbName = item == null ? Sitecore.Context.ContentDatabase.Name : item.Database.Name;
                if (editor.IndexOf("multilist", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    var multiList = new MultilistExtended
                    {
                        ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                        Value = strValue,
                        Database = dbName,
                        ItemID = ItemIDs.RootID.ToString(),
                        Source = variable["Source"] as string ?? "/sitecore",
                    };
                    multiList.SetLanguage(Sitecore.Context.Language.Name);

                    multiList.Class += "  treePicker";
                    return multiList;
                }

                if (editor.IndexOf("droplist", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    if (Sitecore.Context.ContentDatabase?.Name != dbName)
                    {
                        // this control will crash if if content database is different than the items fed to it.
                        return new Literal
                        {
                            Text = "<span style='color: red'>" +
                                   Translate.Text(
                                       Texts
                                           .PowerShellMultiValuePrompt_GetVariableEditor_DropList_control_cannot_render_items_from_the_database___0___because_it_its_not_the_same_as___1___which_is_the_current_content_database__,
                                       dbName, Sitecore.Context.ContentDatabase?.Name) + "</span>",
                            Class = "varHint"
                        };

                    }
                    var lookup = new LookupEx
                    {
                        ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                        Database = dbName,
                        ItemID = item?.ID.ToString() ?? ItemIDs.RootID.ToString(),
                        Source = variable["Source"] as string ?? "/sitecore",
                        ItemLanguage = Sitecore.Context.Language.Name,
                        Value = item?.ID.ToString() ?? ItemIDs.RootID.ToString()
                    };
                    lookup.Class += " textEdit";
                    return lookup;
                }

                if (editor.IndexOf("droptree", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    var tree = new Tree
                    {
                        ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                        Database = dbName,
                        ItemID = item?.ID.ToString() ?? ItemIDs.Null.ToString(),
                        Source = variable["Source"] as string ?? "",
                        ItemLanguage = Sitecore.Context.Language.Name,
                        Value = item?.ID.ToString() ?? ""
                    };
                    tree.Class += " textEdit";
                    return tree;
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
            if (type == typeof(Item) ||
                (!string.IsNullOrEmpty(editor) && (editor.IndexOf("item", StringComparison.OrdinalIgnoreCase) > -1)))
            {
                var item = value as Item;
                var source = variable["Source"] as string;
                var root = variable["Root"] as string;
                var sourceRoot = string.IsNullOrEmpty(source)
                    ? "/sitecore"
                    : StringUtil.ExtractParameter("DataSource", source);
                var dataContext = item != null
                    ? new DataContext
                    {
                        DefaultItem = item.Paths.Path,
                        ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("dataContext"),
                        Parameters = string.IsNullOrEmpty(source) ? "databasename=" + item.Database.Name : source,
                        DataViewName = "Master",
                        Root = string.IsNullOrEmpty(root) ? sourceRoot : root,
                        Database = item.Database.Name,
                        Selected = new[] {new DataUri(item.ID, item.Language, item.Version)},
                        Folder = item.ID.ToString(),
                        Language = item.Language,
                        Version = item.Version
                    }
                    : new DataContext
                    {
                        ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("dataContext"),
                        Parameters = string.IsNullOrEmpty(source) ? "databasename=master" : source,
                        DataViewName = "Master",
                        Root = string.IsNullOrEmpty(root) ? sourceRoot : root
                    };

                DataContextPanel.Controls.Add(dataContext);

                var treePicker = new TreePicker
                {
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                    Value = item != null ? item.ID.ToString() : string.Empty,
                    DataContext = dataContext.ID,
                    AllowNone =
                        !string.IsNullOrEmpty(editor) &&
                        (editor.IndexOf("allownone", StringComparison.OrdinalIgnoreCase) > -1)
                };
                treePicker.Class += " treePicker";
                return treePicker;
            }

            if (type == typeof(bool) ||
                (!string.IsNullOrEmpty(editor) && (editor.IndexOf("bool", StringComparison.OrdinalIgnoreCase) > -1)))
            {
                var checkboxBorder = new Border
                {
                    Class = "checkBoxWrapper",
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_")
                };
                var checkBox = new Checkbox
                {
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                    Header = (string) variable["Title"],
                    HeaderStyle = "display:inline-block;",
                    Checked = (bool) value,
                    Class = "varCheckbox"
                };
                checkboxBorder.Controls.Add(checkBox);
                return checkboxBorder;
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
                    picker.Value = value is IEnumerable<object>
                        ? string.Join("|", ((IEnumerable<object>) value).Select(x => x.ToString()).ToArray())
                        : value.ToString();
                    picker.ExcludeRoles = !showRoles;
                    picker.ExcludeUsers = !showUsers;
                    picker.DomainName = variable["Domain"] as string ?? variable["DomainName"] as string;
                    picker.Multiple = multiple;
                    picker.Click = "UserPickerClick(" + picker.ID + ")";
                    return picker;
                }
            }

            Sitecore.Web.UI.HtmlControls.Control edit;
            if (!string.IsNullOrEmpty(editor) && editor.IndexOf("info", StringComparison.OrdinalIgnoreCase) > -1)
            {
                return new Literal {Text = value.ToString(), Class = "varHint"};
            }

            if (variable["lines"] != null && ((int) variable["lines"] > 1))
            {
                edit = new Memo();
                edit.Attributes.Add("rows", variable["lines"].ToString());
                var placeholder = variable["Placeholder"];
                if (placeholder is string)
                {
                    edit.Attributes.Add("Placeholder", placeholder.ToString());
                }
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
                            // Header = (string) variable["Title"],
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
                        var checkBorder = new Border
                        {
                            Class = "checkListWrapper",
                            ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_")
                        };
                        var editorId = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_");
                        var link =
                            new Literal(
                                @"<div class='checkListActions'>" +
                                @"<a href='#' class='scContentButton' onclick=""javascript:return scForm.postEvent(this,event,'checklist:checkall(id=" +
                                editorId + @")')"">" + Translate.Text("Select all") + "</a> &nbsp;|&nbsp; " +
                                @"<a href='#' class='scContentButton' onclick=""javascript:return scForm.postEvent(this,event,'checklist:uncheckall(id=" +
                                editorId + @")')"">" + Translate.Text("Unselect all") + "</a> &nbsp;|&nbsp;" +
                                @"<a href='#' class='scContentButton' onclick=""javascript:return scForm.postEvent(this,event,'checklist:invert(id=" +
                                editorId + @")')"">" + Translate.Text("Invert selection") + "</a>" +
                                @"</div>");
                        checkBorder.Controls.Add(link);
                        var checkList = new PSCheckList
                        {
                            ID = editorId,
                            HeaderStyle = "margin-top:20px; display:inline-block;",
                            ItemID = ItemIDs.RootID.ToString()
                        };
                        checkList.SetItemLanguage(Sitecore.Context.Language.Name);
                        string[] values;
                        if (value is string)
                        {
                            values = value.ToString().Split('|');
                        }
                        else if (value is IEnumerable)
                        {
                            values =
                                (value as IEnumerable).Cast<object>()
                                    .Select(s => s == null ? "" : s.ToString())
                                    .ToArray();
                        }
                        else
                        {
                            values = new[] {value.ToString()};
                        }
                        foreach (var item in from object option in options.Keys
                            select option.ToString()
                            into optionName
                            let optionValue = options[optionName].ToString()
                            select new ChecklistItem
                            {
                                ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID(checkList.ID),
                                Header = optionName,
                                Value = optionValue,
                                Checked = values.Contains(optionValue, StringComparer.OrdinalIgnoreCase)
                            })
                        {
                            checkList.Controls.Add(item);
                        }

                        checkList.TrackModified = false;
                        checkList.Disabled = false;
                        checkBorder.Controls.Add(checkList);
                        return checkBorder;
                    }
                }

                edit = new Combobox();
                var placeholder = variable["Placeholder"];
                if (placeholder is string)
                {
                    var option = new ListItem
                    {
                        Header = placeholder.ToString(),
                        Value = "",
                        Selected = true
                    };
                    edit.Controls.Add(option);
                }

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
                var placeholder = variable["Placeholder"];
                if (!string.IsNullOrEmpty(editor) && editor.IndexOf("pass", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    edit = new PasswordExtended();
                    if (placeholder is string)
                    {
                        ((PasswordExtended) edit).PlaceholderText = placeholder.ToString();
                    }
                }
                else
                {
                    edit = new EditExtended();
                    if (placeholder is string)
                    {
                        ((EditExtended) edit).PlaceholderText = placeholder.ToString();
                    }
                }
            }
            var tip = variable["Tooltip"] as string;
            if (!string.IsNullOrEmpty(tip))
            {
                edit.ToolTip = tip.RemoveHtmlTags();
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

            var sid = WebUtil.GetQueryString("sid");
            var mandatoryVariables = MandatoryVariables.Split(',').ToList();

            var scriptVariables = GetVariableValues();
            var varsHashtable = new Hashtable();
            var canClose = true;
            var fieldValidators = FieldValidators;
            if (!Validator.IsNullOrEmpty())
            {
                foreach (var variable in scriptVariables)
                {
                    varsHashtable.Add(variable["Name"], variable);
                }
                try
                {
                    using (var session = ScriptSessionManager.GetSession(string.Empty))
                    {
                        session.SetVariable("Variables", varsHashtable);
                        session.ExecuteScriptPart(Validator);
                    }
                }
                catch (Exception ex)
                {
                    PowerShellLog.Error("Error while running form validation script.", ex);
                }

            }

            foreach (var variable in scriptVariables)
            {
                var name = variable["Name"] as string;
                if (string.IsNullOrEmpty(name)) continue;

                if (fieldValidators.ContainsKey(name))
                {
                    var fieldValidator = fieldValidators[name];
                    try
                    {
                        using (var session = ScriptSessionManager.GetSession(string.Empty))
                        {
                            session.SetVariable("variable", variable);
                            session.ExecuteScriptPart(fieldValidator);
                        }
                    }
                    catch (Exception ex)
                    {
                        PowerShellLog.Error($"Error while validating variable: {name}",ex);
                    }
                }

                var error = variable["Error"] as string;
                if (!string.IsNullOrEmpty(error))
                {
                    SheerResponse.SetInnerHtml($"var_{name}_validator", error);
                    canClose = false;
                    continue;
                }
                SheerResponse.SetInnerHtml($"var_{name}_validator", string.Empty);

                if (!mandatoryVariables.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
                var value = variable["Value"];
                var valueType = value?.GetType().Name ?? "null";

                switch (valueType)
                {
                    case "String":
                        if (!string.IsNullOrEmpty(value as string)) continue;
                        break;
                    case "String[]":
                        if ((value as string[]).Length > 0) continue;
                        break;
                    case "DateTime":
                        if ((DateTime) value != DateTime.MinValue && (DateTime) value != DateTime.MaxValue) continue;
                        break;
                    case "null":
                        break;
                    case "List`1":
                        if (value is List<Item>)
                        {
                            var itemList = value as List<Item>;
                            var itemFound = false;
                            foreach (var item in itemList)
                            {
                                if (item != null)
                                {
                                    itemFound = true;
                                }
                            }
                            if (itemFound) continue;
                        }
                        break;
                    default:
                        continue;
                }
                SheerResponse.SetInnerHtml($"var_{name}_validator", "Please provide a value.");
                canClose = false;
            }

            if (!canClose)
            {
                return;
            }

            HttpContext.Current.Cache.Remove(sid);
            HttpContext.Current.Cache[sid] = scriptVariables;
            SheerResponse.SetDialogValue(sid);
            SheerResponse.CloseWindow();
        }

        private Hashtable[] GetVariableValues()
        {
            var results = new List<Hashtable>();
            foreach (Sitecore.Web.UI.HtmlControls.Control control in ValuePanel.Controls)
            {
                if (control is Tabstrip)
                    foreach (WebControl tab in  control.Controls)
                        if (tab is Tab)
                            foreach (WebControl panel in tab.Controls)
                                if (panel is Border)
                                    foreach (Sitecore.Web.UI.HtmlControls.Control editor in panel.Controls)
                                    {
                                        if ("variableWrapper".IsSubstringOf(editor.Class))
                                        {
                                            GetEditorValue(editor, results);
                                        }
                                    }
                GetEditorValue(control, results);
            }
            return results.ToArray();
        }

        private void GetEditorValue(Sitecore.Web.UI.HtmlControls.Control parent, ICollection<Hashtable> results)
        {
            var controlId = parent.ID;
            if (controlId != null && controlId.StartsWith("variable_"))
            {
                foreach (Sitecore.Web.UI.HtmlControls.Control control in parent.Controls)
                {
                    controlId = control.ID;
                    if (controlId != null && controlId.StartsWith("variable_"))
                    {
                        var result = GetVariableValue(control);
                        results.Add(result);
                    }
                }
            }
        }

        private Hashtable GetVariableValue(Sitecore.Web.UI.HtmlControls.Control control)
        {
            var controlId = control.ID;
            var parts = controlId.Split('_');

            var result = new Hashtable(2) {{"Name", string.Join("_", parts.Skip(1).Take(parts.Length - 2).ToArray())}};

            var controlValue = control.Value;
            if (controlValue != null)
            {
                if (control is DateTimePicker)
                {
                    result.Add("Value", DateUtil.IsoDateToDateTime(controlValue));
                }
                else if (control is TreePicker)
                {
                    var picker = control as TreePicker;
                    var contextID = picker.DataContext;
                    var context = (DataContext) DataContextPanel.FindControl(contextID);
                    result.Add("Value", string.IsNullOrEmpty(picker.Value) ? null : context.CurrentItem);
                }
                else if (control is Border && ((Border) control).Class == "checkBoxWrapper")
                {
                    var checkboxBorder = control as Border;
                    foreach (var boolValue in checkboxBorder.Controls.OfType<Checkbox>().Select(ctl => ctl.Checked))
                    {
                        result.Add("Value", boolValue);
                    }
                }
                else if (control is Border && (control as Border).Class == "rulesWrapper")
                {
                    result.Add("Value", Sitecore.Context.ClientPage.ServerProperties[control.ID]);
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
                else if (control is MultilistEx)
                {
                    var multilist = control as MultilistEx;
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
                else if (control is Border && ((Border) control).Class == "checkListWrapper")
                {
                    var checkboxBorder = control as Border;
                    var checkList = checkboxBorder.Controls.OfType<PSCheckList>().FirstOrDefault();
                    var values =
                        checkList?.Controls.Cast<Control>()
                            .Where(item => item is ChecklistItem)
                            .Cast<ChecklistItem>()
                            .Where(checkItem => checkItem.Checked)
                            .Select(checkItem => checkItem.Value)
                            .ToArray();
                    result.Add("Value", values);
                }
                else if (control is Groupbox && control.Class.Contains("scRadioGroup"))
                {
                    foreach (
                        var radioItem in
                        control.Controls.OfType<Radiobutton>()
                            .Select(item => item)
                            .Where(radioItem => radioItem.Checked))
                    {
                        result.Add("Value", radioItem.Value);
                        break;
                    }
                }
                else if (control is Edit || control is Memo)
                {
                    var value = control.Value;
                    var type = GetClrTypeName(control.Class);
                    switch (type)
                    {
                        case "Int16":
                            result.Add("Value", short.Parse(value));
                            break;
                        case "Int32":
                            result.Add("Value", int.Parse(value));
                            break;
                        case "Int64":
                            result.Add("Value", long.Parse(value));
                            break;
                        case "UInt16":
                            result.Add("Value", ushort.Parse(value));
                            break;
                        case "UInt32":
                            result.Add("Value", uint.Parse(value));
                            break;
                        case "UInt64":
                            result.Add("Value", ulong.Parse(value));
                            break;
                        case "Byte":
                            result.Add("Value", byte.Parse(value));
                            break;
                        case "Single":
                            result.Add("Value", float.Parse(value));
                            break;
                        case "Double":
                            result.Add("Value", double.Parse(value));
                            break;
                        case "Decimal":
                            result.Add("Value", decimal.Parse(value));
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
            var typeMatch = TypeRegex.Match(classNames);
            return typeMatch.Success ? typeMatch.Groups["type"].Value : string.Empty;
        }

        protected void CancelClick()
        {
            SheerResponse.CloseWindow();
        }

        protected void EditConditionClick(string id)
        {
            Assert.ArgumentNotNull(id, "id");
            var parameters = new NameValueCollection {["id"] = id};
            Sitecore.Context.ClientPage.Start(this, "EditCondition", parameters);
        }

        protected void EditCondition(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var id = args.Parameters["id"];
            if (string.IsNullOrEmpty(id))
            {
                SheerResponse.Alert("Please select a rule");
            }
            else
            {
                if (!args.IsPostBack)
                {
                    var rule = Sitecore.Context.ClientPage.ServerProperties[id] as string;
                    if (string.IsNullOrEmpty(rule))
                    {
                        rule = "<ruleset />";
                    }

                    var options = new RulesEditorOptions
                    {
                        IncludeCommon = true,
                        RulesPath = "/sitecore/system/Settings/Rules/PowerShell",
                        AllowMultiple = false,
                        Value = rule,
                        HideActions = true,
                    };

                    SheerResponse.ShowModalDialog(options.ToUrlString().ToString(), "580px", "712px", string.Empty, true);
                    args.WaitForPostBack();
                }
                else if (args.HasResult)
                {
                    var content = args.Result;
                    Sitecore.Context.ClientPage.ServerProperties[id] = content;
                    SheerResponse.SetInnerHtml(id + "_renderer", GetRuleConditionsHtml(content));
                }
            }
        }

        private static string GetRuleConditionsHtml(string rule)
        {
            Assert.ArgumentNotNull(rule, "rule");
            var output = new HtmlTextWriter(new StringWriter());
            var renderer2 = new RulesRenderer(rule)
            {
                SkipActions = true,
                AllowMultiple = false
            };
            renderer2.Render(output);
            return output.InnerWriter.ToString();
        }

    }
}