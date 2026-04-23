using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using Sitecore.Data.Items;
using Sitecore.Shell.Applications.ContentEditor;

namespace Spe.Client.Controls
{
    internal class GroupedDroplinkExtended : GroupedDroplink
    {
        public Item[] ScriptedItems { get; set; }

        public bool AllowNone { get; set; }

        public string Placeholder { get; set; }

        protected override void OnLoad(EventArgs e)
        {
            // See LookupExExtended: preserve the initializer-seeded Value across
            // LookupEx.OnLoad's clobber-to-empty on initial render.
            LoadPostData(Value);
            base.OnLoad(e);
        }

        protected override Item[] GetItems(Item current)
            => ScriptedItems ?? base.GetItems(current);

        protected override void DoRender(HtmlTextWriter output)
        {
            if (!AllowNone)
            {
                base.DoRender(output);
                return;
            }

            using (var sw = new StringWriter())
            using (var htw = new HtmlTextWriter(sw))
            {
                base.DoRender(htw);
                var html = sw.ToString();
                var selectedAttr = string.IsNullOrEmpty(Value) ? " selected=\"selected\"" : string.Empty;
                var label = HttpUtility.HtmlEncode(Placeholder ?? string.Empty);
                var placeholderOption = "<option value=\"\"" + selectedAttr + ">" + label + "</option>";
                html = Regex.Replace(html, "(<select[^>]*>)", "$1" + placeholderOption, RegexOptions.IgnoreCase);
                output.Write(html);
            }
        }
    }
}
