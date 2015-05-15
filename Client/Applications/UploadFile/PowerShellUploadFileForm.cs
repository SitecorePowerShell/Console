using System;
using System.Web;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.XmlControls;

namespace Cognifide.PowerShell.Client.Applications.UploadFile
{
    public class PowerShellUploadFileForm : BaseForm
    {
        protected Button CancelButton;
        protected XmlControl Dialog;
        protected Literal DialogDescription;
        protected Literal DialogHeader;
        protected GenericControl ItemUri;
        protected GenericControl LanguageName;
        protected Button OKButton;
        protected GenericControl Overwrite;
        protected GenericControl Unpack;
        protected GenericControl Versioned;

        protected void EndUploading(string id)
        {
            ID realId;
            SheerResponse.SetDialogValue(ID.TryParse(id, out realId) ? id : HttpUtility.UrlDecode(id));
            SheerResponse.CloseWindow();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent && !Context.ClientPage.IsPostBack)
            {
                var handle = UrlHandle.Get();
                var path = handle["path"];
                if (path == null)
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

                Versioned.Attributes["value"] = handle["versioned"];
                LanguageName.Attributes["value"] = handle["language"];
                Overwrite.Attributes["value"] = handle["overwrite"];
                Unpack.Attributes["value"] = handle["unpack"];
                var title = handle["te"];
                if (!string.IsNullOrEmpty(title))
                {
                    DialogHeader.Text = WebUtil.SafeEncode(title);
                }

                var message = handle["ds"];
                if (!string.IsNullOrEmpty(message))
                {
                    DialogDescription.Text = WebUtil.SafeEncode(message);
                }

                var ok = handle["ok"];
                if (!string.IsNullOrEmpty(ok))
                {
                    OKButton.Header = WebUtil.SafeEncode(ok);
                }

                var cancel = handle["cancel"];
                if (!string.IsNullOrEmpty(cancel))
                {
                    CancelButton.Header = WebUtil.SafeEncode(cancel);
                }
            }
        }

        protected void OKClick()
        {
            var str = Context.ClientPage.ClientRequest.Form["File"];
            if ((str == null) || (str.Trim().Length == 0))
            {
                SheerResponse.Alert("Specify a file to upload.");
            }
            else
            {
                OKButton.Disabled = true;
                CancelButton.Disabled = true;
                Context.ClientPage.ClientResponse.Timer("StartUploading", 10);
            }
        }

        protected void CancelClick()
        {
            SheerResponse.CloseWindow();
        }

        protected void ShowError()
        {
            SheerResponse.Alert(
                "An error occured while uploading a file .\n\nThe reason may be that the file does not exist or the path is wrong.");
            OKButton.Disabled = true;
            CancelButton.Disabled = true;
            OKButton.Disabled = false;
            CancelButton.Disabled = false;
        }

        protected void ShowFileTooBig()
        {
            SheerResponse.Alert(
                Translate.Text(
                    "The file is too big to be uploaded.\n\nThe maximum size of a file that can be uploaded is {0}.",
                    MainUtil.FormatSize(Settings.Upload.MaximumDatabaseUploadSize)));
            OKButton.Disabled = true;
            CancelButton.Disabled = true;
            OKButton.Disabled = false;
            CancelButton.Disabled = false;
        }

        protected void ShowFileTooBig(string filename)
        {
            Assert.ArgumentNotNullOrEmpty(filename, "filename");
            SheerResponse.Alert(
                Translate.Text(
                    "The file \"{0}\" is too big to be uploaded.\n\nThe maximum size of a file that can be uploaded is {1}.",
                    filename, MainUtil.FormatSize(Settings.Upload.MaximumDatabaseUploadSize)));
            OKButton.Disabled = true;
            CancelButton.Disabled = true;
            OKButton.Disabled = false;
            CancelButton.Disabled = false;
        }

        protected void StartUploading()
        {
            Context.ClientPage.ClientResponse.Eval("submit()");
        }
    }
}