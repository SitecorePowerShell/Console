using System;
using System.Web;
using Cognifide.PowerShell.Abstractions.VersionDecoupling.Interfaces;
using Cognifide.PowerShell.Core.VersionDecoupling;
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
        protected Literal UploadWarning;
        protected Image DialogIcon;
        protected GenericControl ItemUri;
        protected GenericControl LanguageName;
        protected Button OKButton;
        protected GenericControl Overwrite;
        protected GenericControl Unpack;
        protected GenericControl Versioned;

        protected void EndUploading(string id)
        {
            SheerResponse.SetDialogValue(ID.TryParse(id, out _) ? id : HttpUtility.UrlDecode(id));
            SheerResponse.CloseWindow();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (Context.ClientPage.IsEvent || Context.ClientPage.IsPostBack) return;

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

            var icon = handle["ic"];
            if (!string.IsNullOrEmpty(icon))
            {
                DialogIcon.Src = WebUtil.SafeEncode(icon);
                DialogIcon.Visible = true;
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
            TypeResolver.Resolve<IUrlHandleWrapper>().DisposeHandle(handle);
        }

        protected void OKClick()
        {
            var str = Context.ClientPage.ClientRequest.Form["File"];
            if (str == null || str.Trim().Length == 0)
            {
                UploadWarning.Text = Sitecore.Texts.PLEASE_SPECIFY_A_FILE_TO_UPLOAD;
            }
            else
            {
                UploadWarning.Text = string.Empty;
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
            SheerResponse.Alert(Sitecore.Texts.AN_ERROR_OCCURED_WHILE_UPLOADING_THE_REASON_MAY_BE_THAT_THE_FILE_DOES_NOT_EXIST);
            OKButton.Disabled = true;
            CancelButton.Disabled = true;
            OKButton.Disabled = false;
            CancelButton.Disabled = false;
        }

        protected void ShowFileTooBig()
        {
            SheerResponse.Alert(
                Translate.Text(Sitecore.Texts.THE_FILE_IS_TOO_BIG_TO_BE_UPLOADED_THE_MAXIMUM_SIZE_FOR_UPLOADING_FILES_IS_0, MainUtil.FormatSize(Settings.Upload.MaximumDatabaseUploadSize))
            );
            OKButton.Disabled = true;
            CancelButton.Disabled = true;
            OKButton.Disabled = false;
            CancelButton.Disabled = false;
        }

        protected void ShowFileTooBig(string filename)
        {
            Assert.ArgumentNotNullOrEmpty(filename, "filename");
            SheerResponse.Alert(
                Translate.Text(Sitecore.Texts.THE_FILE_0_IS_TOO_BIG_TO_BE_UPLOADED_THE_MAXIMUM_SIZE_FOR_UPLOADING_FILES_IS_1, filename, MainUtil.FormatSize(Settings.Upload.MaximumDatabaseUploadSize))
            );
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