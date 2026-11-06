using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.UI;
using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.Rules;
using Combobox = Sitecore.Web.UI.HtmlControls.Combobox;
using Spe.Core.Extensions;

namespace Spe.Client.Controls.VariableEditors
{
    internal static class VariableEditorHelper
    {
        private static readonly Regex TypeRegex = new Regex(@".*clr(?<type>[\w-\.]+)\s*",
            RegexOptions.Singleline | RegexOptions.Compiled);

        public static OrderedDictionary ParseOptions(IDictionary variable)
        {
            var psOptions = variable["Options"].BaseObject();
            var options = new OrderedDictionary();
            switch (psOptions)
            {
                case OrderedDictionary _:
                    options = psOptions as OrderedDictionary;
                    break;
                case string _:
                    var strOptions = ((string)variable["Options"]).Split('|');
                    var i = 0;
                    while (i < strOptions.Length)
                    {
                        options.Add(strOptions[i++], strOptions[i++]);
                    }
                    break;
                case Hashtable _:
                    var hashOptions = variable["Options"] as Hashtable;
                    foreach (var key in hashOptions.Keys)
                    {
                        options.Add(key, hashOptions[key]);
                    }
                    break;
                default:
                    throw new Exception("Checklist options format unrecognized.");
            }
            return options;
        }

        public static OrderedDictionary ParseOptionTooltips(IDictionary variable)
        {
            var optionTooltips = new OrderedDictionary();
            if (variable["OptionTooltips"] == null) return optionTooltips;

            var psOptionTooltips = variable["OptionTooltips"].BaseObject();
            if (psOptionTooltips is OrderedDictionary dictionary)
            {
                optionTooltips = dictionary;
            }
            else if (psOptionTooltips is Hashtable)
            {
                var hashOptions = variable["OptionTooltips"] as Hashtable;
                foreach (var key in hashOptions.Keys)
                {
                    optionTooltips.Add(key, hashOptions[key]);
                }
            }
            return optionTooltips;
        }

        public static void ApplyTextDefaults(Sitecore.Web.UI.HtmlControls.Control edit, IDictionary variable, object value)
        {
            var name = (string)variable["Name"];
            var tip = variable["Tooltip"] as string;
            if (!string.IsNullOrEmpty(tip))
            {
                edit.ToolTip = tip.RemoveHtmlTags();
            }
            edit.Style.Add("float", "left");
            edit.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_");
            edit.Class += " scContentControl textEdit clr" + value.GetType().FullName.Replace(".", "-");
            edit.Value = value.ToString();

            if (variable["MaxLength"] is int maxLength && maxLength > 0 && !(edit is Combobox))
            {
                edit.Attributes["maxlength"] = maxLength.ToString();
                edit.Attributes["data-maxlength"] = maxLength.ToString();
            }
        }

        public static void ReadTextValue(Sitecore.Web.UI.HtmlControls.Control control, Hashtable result)
        {
            var value = control.Value;
            var typeName = GetClrTypeName(control.Class);
            result.Add("Value", TryParse(value, typeName));
        }

        public static string GetClrTypeName(string classNames)
        {
            var typeMatch = TypeRegex.Match(classNames);
            return typeMatch.Success ? typeMatch.Groups["type"].Value.Replace("-", ".") : string.Empty;
        }

        public static object TryParse(string inputValue, string typeName)
        {
            try
            {
                var targetType = Type.GetType(typeName);
                return typeof(IConvertible).IsAssignableFrom(targetType)
                    ? Convert.ChangeType(inputValue, Type.GetType(typeName))
                    : inputValue;
            }
            catch
            {
                return null;
            }
        }

        public static void AddControlAttributes(IDictionary variable, Sitecore.Web.UI.HtmlControls.Control variableEditor)
        {
            if (!variable.Contains("GroupId")) return;

            variableEditor.Attributes.Add("data-group-id", variable["GroupId"].ToString());
        }

        public static string GetRuleConditionsHtml(string rule, bool showActions = false)
        {
            Assert.ArgumentNotNull(rule, "rule");
            var output = new HtmlTextWriter(new StringWriter());
            var renderer = new RulesRenderer(rule)
            {
                SkipActions = !showActions,
                AllowMultiple = false
            };
            renderer.Render(output);
            return output.InnerWriter.ToString();
        }
    }
}
