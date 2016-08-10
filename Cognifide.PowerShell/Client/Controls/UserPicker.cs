using System;
using System.Linq;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore.Security;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Controls
{
    public class UserPicker : Control
    {
        protected Button PickButton;
        protected Edit Viewer;

        public UserPicker()
        {
            Viewer = new Edit
            {
                ReadOnly = true,
                Class = "scUserPickerEdit textEdit clrString",
                ID = GetUniqueID("edit_")
            };
            PickButton = new Button
            {
                Header = "...", 
                Class = "scUserPickerButton"
            };
            Controls.Add(Viewer);
            Controls.Add(PickButton);
        }

        public string Click
        {
            get { return PickButton.Click; }
            set { PickButton.Click = value; }
        }

        public override string Value
        {
            get { return base.Value; }
            set
            {
                Viewer.Value = string.Empty;
                var result = string.Empty;
                var entries = value.Split('|');
                foreach (var entryParts in entries.Select(entry => entry.Split('^')))
                {
                    Viewer.Value += entryParts[0] + ", ";
                    result += entryParts[0] + "|";
                }
                Viewer.Value = Viewer.Value.TrimEnd(',', ' ');
                result = result.Trim('|');
                base.Value = result;
                SheerResponse.SetAttribute(Viewer.ID, "value", Viewer.Value);
            }
        }

        public bool ExcludeUsers
        {
            get { return GetViewStateBool("ExcludeUsers"); }
            set { SetViewStateBool("ExcludeUsers", value); }
        }

        public bool ExcludeRoles
        {
            get { return GetViewStateBool("ExcludeRoles"); }
            set { SetViewStateBool("ExcludeRoles", value); }
        }

        public string DomainName
        {
            get { return GetViewStateString("DomainName"); }
            set { SetViewStateString("DomainName", value); }
        }

        public bool Multiple
        {
            get { return GetViewStateBool("Multiple"); }
            set { SetViewStateBool("Multiple", value); }
        }

        public void Clicked(ClientPipelineArgs args)
        {
            if (!args.IsPostBack)
            {
                var options = new SelectAccountOptions
                {
                    Multiple = Multiple,
                    ExcludeUsers = ExcludeUsers,
                    ExcludeRoles = ExcludeRoles
                };
                if (!string.IsNullOrEmpty(DomainName))
                {
                    options.DomainName = DomainName;
                }
                SheerResponse.ShowModalDialog(options.ToUrlString().ToString(), true);
                args.WaitForPostBack();
            }
            else
            {
                if (args.Result == null) return;

                Value = (args.HasResult) ? args.Result : String.Empty;
                Sitecore.Context.ClientPage.ClientResponse.Refresh(Parent);
                SitecoreVersion.V81.OrOlder(() =>
                    Sitecore.Context.ClientPage.ClientResponse.Eval("ResizeDialogControls();")
                    );
            }
        }
    }
}