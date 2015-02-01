using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.XmlControls;
using System;
using System.Web;
using Sitecore.Reflection;

namespace Cognifide.PowerShell.Client.Applications.UploadFile
{
    public class PowerShellUploadFileForm : DialogForm
    {
        protected GenericControl ItemUri;
        protected XmlControl Dialog;

        protected void EndUploading(string id)
        {
            ID realId;
            if (ID.TryParse(id, out realId))
            {
                SheerResponse.SetDialogValue(id);
            }
            else
            {
                SheerResponse.SetDialogValue(HttpUtility.UrlDecode(id));
            }
            base.OnOK(this, EventArgs.Empty);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent && !Context.ClientPage.IsPostBack)
            {
                UrlHandle handle = UrlHandle.Get();
                var path = handle["path"];
                if ( path== null)
                {
                    var uri = Sitecore.Data.ItemUri.ParseQueryString(Context.ContentDatabase);
                    var item = Database.GetItem(uri);
                    if (item != null)
                    {
                        ItemUri.Attributes["value"] = item.Uri.ToString();
                    }
                }
                else
                {
                    ItemUri.Attributes["value"] = path;
                }

                string title = handle["h"];
                if (!string.IsNullOrEmpty(title))
                {
                    ReflectionUtil.SetProperty(this, "Header", WebUtil.SafeEncode(title));
                    //Dialog["Header"] = WebUtil.SafeEncode(title);
                    SheerResponse.Eval("setDialogValue('.DialogHeader', 'new title');");
                    SheerResponse.Eval("setDialogValue('.DialogHeaderDescription', 'new description');");
                    
                }
                string message = handle["t"];
                if (!string.IsNullOrEmpty(message))
                {
                    //Dialog["Text"] = WebUtil.SafeEncode(message);
                    ReflectionUtil.SetProperty(this, "Text", WebUtil.SafeEncode(message));
                }
            }
        }

        protected override void OnOK(object sender, EventArgs args)
        {
            var str = Context.ClientPage.ClientRequest.Form["File"];
            if ((str == null) || (str.Trim().Length == 0))
            {
                SheerResponse.Alert("Specify a file to upload.");
            }
            else
            {
                OK.Disabled = true;
                Cancel.Disabled = true;
                Context.ClientPage.ClientResponse.Timer("StartUploading", 10);
            }
        }

        protected void ShowError()
        {
            SheerResponse.Alert(
                "An error occured while uploading a file .\n\nThe reason may be that the file does not exist or the path is wrong.");
            OK.Disabled = true;
            Cancel.Disabled = true;
            OK.Disabled = false;
            Cancel.Disabled = false;
        }

        protected void ShowFileTooBig()
        {
            SheerResponse.Alert(
                Translate.Text(
                    "The file is too big to be uploaded.\n\nThe maximum size of a file that can be uploaded is {0}.",
                    MainUtil.FormatSize(Settings.Upload.MaximumDatabaseUploadSize)));
            OK.Disabled = true;
            Cancel.Disabled = true;
            OK.Disabled = false;
            Cancel.Disabled = false;
        }

        protected void ShowFileTooBig(string filename)
        {
            Assert.ArgumentNotNullOrEmpty(filename, "filename");
            SheerResponse.Alert(
                Translate.Text(
                    "The file \"{0}\" is too big to be uploaded.\n\nThe maximum size of a file that can be uploaded is {1}.",
                    filename, MainUtil.FormatSize(Settings.Upload.MaximumDatabaseUploadSize)));
            OK.Disabled = true;
            Cancel.Disabled = true;
            OK.Disabled = false;
            Cancel.Disabled = false;
        }

        protected void StartUploading()
        {
            Context.ClientPage.ClientResponse.Eval("submit()");
        }
    }
}