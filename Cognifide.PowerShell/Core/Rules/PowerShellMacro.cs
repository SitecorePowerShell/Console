using System;
using System.Globalization;
using System.Web;
using System.Xml.Linq;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Rules.RuleMacros;
using Sitecore.Text;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;

namespace Cognifide.PowerShell.Core.Rules
{
    public class PowerShellMacro: DialogForm, IRuleMacro
    {

        public void Execute(XElement element, string name, UrlString parameters, string value)
        {
            Assert.ArgumentNotNull(element, "element");
            Assert.ArgumentNotNull(name, "name");
            Assert.ArgumentNotNull(parameters, "parameters");
            Assert.ArgumentNotNull(value, "value");
            string scriptId = parameters["script"];
            try
            {
                var scriptItem = Sitecore.Client.ContentDatabase.GetItem(scriptId);
                var itemStr =  XElement.Parse(element.ToString()).FirstAttribute.Value;
                var currentItem = Sitecore.Client.ContentDatabase.GetItem(itemStr);
                if (scriptItem.InheritsFrom(Templates.Script.Id))
                {
                    var scriptItemField = scriptItem.Fields[Templates.Script.Fields.ScriptBody];

                    string script = scriptItemField.GetValue(false);
                    if (!string.IsNullOrEmpty(script))
                    {
                        //todo verify that readVariables opens a prompt
                        var str = new UrlString(UIUtil.GetUri("control:PowerShellRunner"));
                        var scriptResultKey = Guid.NewGuid().ToString();
                        if (currentItem != null)
                        {
                            str.Append("id", currentItem.ID.ToString());
                            str.Append("db", currentItem.Database.Name);
                            str.Append("ver", currentItem.Version.Number.ToString(CultureInfo.InvariantCulture));
                            str.Append("lang", currentItem.Language.Name);
                        }
                        str.Append("scriptId", scriptItem.ID.ToString());
                        str.Append("scriptDb", scriptItem.Database.Name);
                        str.Append("sessionKey", scriptResultKey);
                        var clientPageClientResponse = Context.ClientPage.ClientResponse;
                        clientPageClientResponse.EnableOutput();
                        clientPageClientResponse.ShowModalDialog(str.ToString(), "400", "260", "PowerShell Script Results", true);
                        string scriptResult = HttpContext.Current.Session[scriptResultKey].ToString();
                        clientPageClientResponse.SetReturnValue(scriptResult);
                    }
                    else
                    {
                        Log.Warn("Selected Script Item is empty", this);
                    }
                }
                else
                {
                    Log.Warn("Selected Item is not a Script", this);
                }

            }
            catch (Exception ex)
            {
                Log.Error("Error in Boolean Script Rule", ex, this);
                throw;
            }
        }
    }
}