using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Sitecore;
using Sitecore.Controls;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.Dialogs.RulesEditor;
using Sitecore.StringExtensions;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Spe.Client.Controls;
using Spe.Client.Controls.VariableEditors;
using Spe.Commands.Interactive.Messages;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Host;
using Button = Sitecore.Web.UI.HtmlControls.Button;
using Control = System.Web.UI.Control;
using DateTime = System.DateTime;
using Label = Sitecore.Web.UI.HtmlControls.Label;
using Literal = Sitecore.Web.UI.HtmlControls.Literal;

namespace Spe.Client.Applications
{
    public class PowerShellMultiValuePrompt : DialogPage
    {
        private static readonly List<IVariableEditor> Editors = new List<IVariableEditor>
        {
            new DateTimeVariableEditor(),
            new RuleVariableEditor(),
            new ListVariableEditor(),
            new ItemVariableEditor(),
            new BoolVariableEditor(),
            new SecurityVariableEditor(),
            new InfoVariableEditor(),
            new MarqueeVariableEditor(),
            new MemoVariableEditor(),
            new LinkVariableEditor(),
            new RadioVariableEditor(),
            new CheckboxListVariableEditor(),
            new ComboboxVariableEditor(),
            new TextVariableEditor()
        };

        protected Button CancelButton;
        protected Border DataContextPanel;
        protected Literal DialogDescription;
        protected Literal DialogHeader;
        protected ThemedImage DialogIcon;
        protected Literal CustomStyles;
        protected Button OKButton;
        protected Literal Result;
        protected bool ShowHints;
        protected Literal TabOffsetValue;
        protected Tabstrip Tabstrip;
        protected Scrollbox ValuePanel;
        protected Border NoDataWarning;

        public static string MandatoryVariables
        {
            get => StringUtil.GetString(Sitecore.Context.ClientPage.ServerProperties["MandatoryVariables"]);
            set => Sitecore.Context.ClientPage.ServerProperties["MandatoryVariables"] = value;
        }

        public static string Validator
        {
            get => StringUtil.GetString(Sitecore.Context.ClientPage.ServerProperties["Validator"]);
            set => Sitecore.Context.ClientPage.ServerProperties["Validator"] = value;
        }

        public Hashtable ValidatorParameters
        {
            get => (Hashtable)Sitecore.Context.ClientPage.ServerProperties["ValidatorParameters"];
            set => Sitecore.Context.ClientPage.ServerProperties["ValidatorParameters"] = value;
        }

        public static Dictionary<string, string> FieldValidators
        {
            get
            {
                var fv = (Dictionary<string, string>)Sitecore.Context.ClientPage.ServerProperties["FieldValidators"];
                if (fv != null) return fv;

                fv = new Dictionary<string, string>();
                Sitecore.Context.ClientPage.ServerProperties["FieldValidators"] = fv;
                return fv;
            }
            set => Sitecore.Context.ClientPage.ServerProperties["FieldValidators"] = value;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Tabstrip.OnTabClicked += Tabstrip_OnChange;

            if (Sitecore.Context.ClientPage.IsEvent)
                return;

            HttpContext.Current.Response.AddHeader("X-UA-Compatible", "IE=edge");
            var sid = WebUtil.GetQueryString("sid");

            if (!(ScriptSessionManager.GetSessionIfExists(sid) is ScriptSession scriptSession) ||
                !scriptSession.DialogStack.Any() ||
                !(scriptSession.DialogStack.Peek() is ShowMultiValuePromptMessage message))
            {
                DialogDescription.Text = "&nbsp;";
                return;
            }

            NoDataWarning.Visible = false;

            var variables = message.Parameters;
            var title = message.Title;
            ShowHints = message.ShowHints;

            if (!string.IsNullOrEmpty(title))
            {
                DialogHeader.Text = title;
            }

            var icon = message.Icon;
            if (!string.IsNullOrEmpty(icon))
            {
                DialogIcon.Src = icon;
                DialogIcon.Visible = true;
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
            ValidatorParameters = message.ValidatorParameters;

            //MandatoryVariables =
            var mandatoryVariables =
                variables.Cast<Hashtable>()
                    .Where(p => p["Mandatory"] is bool && (bool)p["Mandatory"])
                    .Select(v => v["Name"]).ToList();
            if (mandatoryVariables.Any())
            {
                MandatoryVariables = (string)mandatoryVariables.Aggregate((accumulated, next) =>
                       next + "," + accumulated);
            }
            AddControls(variables);
            CustomStyles.Text = "<style>.scContentButton { color: rgb(38, 148, 192);}</style>";
        }

        private void AddControls(object[] variables)
        {
            var tabs = new Dictionary<string, Border>(StringComparer.OrdinalIgnoreCase);
            Tabstrip.Visible =
                variables.Cast<Hashtable>()
                    .Select(variable => variable["Tab"] as string)
                    .Any(tabName => !string.IsNullOrEmpty(tabName));
            var fieldValidators = FieldValidators;
            var fieldNames = new HashSet<string>();
            foreach (Hashtable variable in variables)
            {
                var isVisible = MainUtil.GetBool(variable["Visible"], true);
                if (!isVisible) continue;

                var tabName = variable["Tab"] as string ?? "Other";
                var name = variable["Name"] as string ?? string.Empty;
                var title = variable["Title"] as string ?? name;
                var hint = variable["Tip"] as string ??
                           variable["Hint"] as string ?? variable["Tooltip"] as string;
                var variableValue = variable["Value"];
                var columns = 12;
                var clearfix = false;
                var floatRight = false;
                var height = variable["Height"] as string;
                var validator = variable["Validator"] as ScriptBlock;

                if (fieldNames.Contains(name))
                {
                    PowerShellLog.Error($"[Dialog] action=evaluateVariables status=failed reason=duplicateName name={name}");
                    throw new ArgumentException("A duplicate variable name encountered while calling Read-Variable.", "name");
                }

                fieldNames.Add(name);

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
                    floatRight = strColumns.Contains("last", StringComparer.OrdinalIgnoreCase);
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
                    PowerShellLog.Error($"[Dialog] action=renderEditor status=failed variable=\"{title}\"", ex);
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
                    if (!string.IsNullOrEmpty(title))
                    {
                        var fieldLabel = new Label { Header = title, Class = "varTitle", For = name };
                        variableWrapper.Controls.Add(fieldLabel);
                    }

                    if (ShowHints && !string.IsNullOrEmpty(hint))
                    {
                        //var hintLabel = new Label { Header = hint, Class = "varHint" };
                        var hintLabel = new Literal(hint)
                        {
                            Class = "varHint"
                        };

                        variableWrapper.Controls.Add(hintLabel);
                    }
                }

                if (!string.IsNullOrEmpty(height))
                {
                    variableWrapper.Height = new Unit(height);
                    variableWrapper.Style.Add("float", "none");
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
                if (floatRight)
                {
                    variableWrapper.Class += " floatRight";
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

                AddRelatedAttributes(variable, variableWrapper);

                // add wrapper to the container
                container.Controls.Add(variableWrapper);
            }

            FieldValidators = fieldValidators;
            TabOffsetValue.Text = $"<script type='text/javascript'>var tabsOffset={(tabs.Count > 0 ? 24 : 0)};</script>";
        }

        public void Tabstrip_OnChange(object sender, EventArgs e)
        {
        }

        private WebControl GetContainer(IDictionary<string, Border> tabs, string tabName)
        {
            if (!Tabstrip.Visible)
            {
                return ValuePanel;
            }

            if (tabs.ContainsKey(tabName)) return tabs[tabName];

            var tab = new Tab { Header = tabName, ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("tab_") };
            Tabstrip.Controls.Add(tab);
            Tabstrip.Width = new Unit("100%");
            var border = new Border();
            tab.Controls.Add(border);
            tabs.Add(tabName, border);
            return border;
        }

        private static void AddRelatedAttributes(IDictionary variable, Sitecore.Web.UI.HtmlControls.Control variableEditor)
        {
            if (!variable.Contains("ParentGroupId")) return;

            variableEditor.Attributes.Add("data-parent-group-id", variable["ParentGroupId"]?.ToString());
            if (variable["HideOnValue"] != null)
            {
                variableEditor.Attributes.Add("data-hide-on-value", variable["HideOnValue"]?.ToString());
            } 
            else if (variable["ShowOnValue"] != null)
            {
                variableEditor.Attributes.Add("data-show-on-value", variable["ShowOnValue"]?.ToString());
            }
        }

        private Control GetVariableEditor(IDictionary variable)
        {
            var value = variable["Value"].BaseObject();
            var editor = variable["Editor"] as string;
            var valueType = value.GetType();
            var editorContext = new VariableEditorContext { DataContextPanel = DataContextPanel };

            foreach (var variableEditor in Editors)
            {
                if (variableEditor.CanHandle(variable, editor, valueType))
                {
                    return variableEditor.CreateControl(variable, editorContext);
                }
            }

            return new Literal { Text = value.ToString(), Class = "varHint" };
        }

        protected void UserPickerClick()
        {
            var requestParams = Sitecore.Context.ClientPage.Request.Params["__PARAMETERS"];
            if (requestParams.StartsWith("UserPickerClick("))
            {
                var controlId = requestParams.Substring(16, requestParams.IndexOf(')') - 16);
                if (ValuePanel.FindControl(controlId) is UserPicker picker)
                {
                    Sitecore.Context.ClientPage.Start(picker, "Clicked");
                }
            }
        }

        protected void OKClick()
        {            
            var sid = WebUtil.SafeEncode(WebUtil.GetQueryString("sid"));
            var mandatoryVariables = MandatoryVariables.Split(',').ToList();

            var scriptVariables = GetVariableValues();

            var xssCleanup = new Regex(@"<script[^>]*>[\s\S]*?</script>|<noscript[^>]*>[\s\S]*?</noscript>|<img.*onerror.*>");
            foreach (var variable in scriptVariables)
            {
                if (variable["Value"] != null &&
                    xssCleanup.IsMatch(variable["Value"].ToString()))
                {
                    SheerResponse.Alert(Texts.PowerShellMultiValuePrompt_InsecureData_error);
                    return;
                }
            }

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
                        SetValidatorParameters(session);
                        session.ExecuteScriptPart(Validator);
                    }
                }
                catch (Exception ex)
                {
                    PowerShellLog.Error("[Dialog] action=validateForm status=failed", ex);
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
                            SetValidatorParameters(session);
                            session.ExecuteScriptPart(fieldValidator);
                        }
                    }
                    catch (Exception ex)
                    {
                        PowerShellLog.Error($"[Dialog] action=validateField status=failed name={name}", ex);
                    }
                }

                var error = variable["Error"] as string;
                if (!string.IsNullOrEmpty(error))
                {
                    SheerResponse.SetInnerHtml($"var_{name}_validator", WebUtil.SafeEncode(error));
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
                        var arr = value as string[];
                        if (arr.Length > 0 && arr.Any(s => !string.IsNullOrEmpty(s))) continue;
                        break;
                    case "DateTime":
                        if ((DateTime)value != DateTime.MinValue && (DateTime)value != DateTime.MaxValue) continue;
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

            if (ScriptSessionManager.GetSessionIfExists(sid) is ScriptSession scriptSession &&
                scriptSession.DialogStack.Any() &&
                scriptSession.DialogStack.Peek() is ShowMultiValuePromptMessage)
            {
                scriptSession.DialogResults = scriptVariables;
            }

            SheerResponse.SetDialogValue(sid);
            SheerResponse.CloseWindow();
        }

        private void SetValidatorParameters(ScriptSession session)
        {
            var valParams = ValidatorParameters;
            if (valParams == null) return;

            foreach (var valParam in valParams.Keys)
            {
                session.SetVariable(valParam.ToString(), valParams[valParam]);
            }
        }

        private Hashtable[] GetVariableValues()
        {
            var results = new List<Hashtable>();
            foreach (Sitecore.Web.UI.HtmlControls.Control control in ValuePanel.Controls)
            {
                if (control is Tabstrip)
                    foreach (WebControl tab in control.Controls)
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
            if (controlId == null || !controlId.StartsWith("variable_")) return;

            foreach (var childControl in parent.Controls)
            {
                if (!(childControl is Sitecore.Web.UI.HtmlControls.Control control)) continue;

                controlId = control.ID;
                if (controlId == null || !controlId.StartsWith("variable_")) continue;

                var result = GetVariableValue(control);
                results.Add(result);
            }
        }

        private Hashtable GetVariableValue(Sitecore.Web.UI.HtmlControls.Control control)
        {
            var controlId = control.ID;
            var parts = controlId.Split('_');

            var result = new Hashtable(2) { { "Name", string.Join("_", parts.Skip(1).Take(parts.Length - 2).ToArray()) } };

            var controlValue = control.Value;
            if (controlValue == null) return result;

            var editorContext = new VariableEditorContext { DataContextPanel = DataContextPanel };

            foreach (var variableEditor in Editors)
            {
                if (variableEditor.CanReadValue(control))
                {
                    variableEditor.ReadValue(control, result, editorContext);
                    return result;
                }
            }

            return result;
        }

        protected void CancelClick()
        {
            SheerResponse.CloseWindow();
        }

        protected void EditConditionClick(string id, string showActions, string rulespath)
        {
            Assert.ArgumentNotNull(id, "id");
            var parameters = new NameValueCollection { ["id"] = id, ["showActions"] = showActions, ["rulespath"] = rulespath };

            Sitecore.Context.ClientPage.Start(this, "EditCondition", parameters);
        }

        protected void EditCondition(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var id = args.Parameters["id"];
            if (string.IsNullOrEmpty(id))
            {
                SheerResponse.Alert(Texts.PowerShellMultiValuePrompt_EditCondition_Please_select_a_rule);
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

                    var hideActions = !MainUtil.GetBool(args.Parameters["showActions"], false);
                    var rulesPath = !string.IsNullOrEmpty(args.Parameters["rulespath"])
                        ? args.Parameters["rulespath"]
                        : "/sitecore/system/Settings/Rules/PowerShell";
                    var options = new RulesEditorOptions
                    {
                        IncludeCommon = true,
                        RulesPath = rulesPath,
                        AllowMultiple = false,
                        Value = rule,
                        HideActions = hideActions
                    };

                    SheerResponse.ShowModalDialog(options.ToUrlString().ToString(), hideActions ? "580px" : "1000px", "712px", string.Empty, true);
                    args.WaitForPostBack();
                }
                else if (args.HasResult)
                {
                    var content = args.Result;
                    var hideActions = !MainUtil.GetBool(args.Parameters["showActions"], false);
                    Sitecore.Context.ClientPage.ServerProperties[id] = content;
                    SheerResponse.SetInnerHtml(id + "_renderer", VariableEditorHelper.GetRuleConditionsHtml(content, !hideActions));
                }
            }
        }

    }
}