using System;
using System.Web;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Sitecore.Pipelines.Upload;
using Sitecore.Shell.Web.UI;
using Sitecore.Web;
using Sitecore.Web.UI.XmlControls;

namespace Cognifide.PowerShell.Client.Applications.UploadFile
{
    public class PowerShellUploadFilePage2 : SecurePage
    {
        protected override void OnInit(EventArgs e)
        {
            var control = ControlFactory.GetControl("PowerShellUploadFile");
            if (control != null)
                Controls.Add(control);
            base.OnInit(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (MaxRequestLengthExceeded)
            {
                HttpContext.Current.Response.Write(
                    "<html><head><script type=\"text/JavaScript\" language=\"javascript\">window.top.scForm.getTopModalDialog().frames[0].scForm.postRequest(\"\", \"\", \"\", 'ShowFileTooBig()')</script></head><body>Done</body></html>");
            }
            else
            {
                if (IsEvent)
                    return;
                if (Request.Files.Count <= 0)
                    return;
                try
                {
                    var pathOrId = Sitecore.Context.ClientPage.ClientRequest.Form["ItemUri"];
                    var langStr = Sitecore.Context.ClientPage.ClientRequest.Form["LanguageName"];
                    var language = langStr.Length > 0
                        ? LanguageManager.GetLanguage(langStr) ?? Sitecore.Context.ContentLanguage
                        : Sitecore.Context.ContentLanguage;
                    var itemUri = ItemUri.Parse(pathOrId);
                    var uploadArgs = new UploadArgs();
                    if (itemUri != null)
                    {
                        pathOrId = itemUri.GetPathOrId();
                        uploadArgs.Destination = Settings.Media.UploadAsFiles
                            ? UploadDestination.File
                            : UploadDestination.Database;
                    }
                    else
                    {
                        uploadArgs.Destination = UploadDestination.File;
                        uploadArgs.FileOnly = true;
                    }
                    uploadArgs.Files = Request.Files;
                    uploadArgs.Folder = pathOrId;
                    uploadArgs.Overwrite = Sitecore.Context.ClientPage.ClientRequest.Form["Overwrite"].Length > 0;
                    //uploadArgs.Overwrite = Settings.Upload.SimpleUploadOverwriting;
                    uploadArgs.Unpack = Sitecore.Context.ClientPage.ClientRequest.Form["Unpack"].Length > 0;
                    uploadArgs.Versioned = Sitecore.Context.ClientPage.ClientRequest.Form["Versioned"].Length > 0;
                    //uploadArgs.Versioned = Settings.Media.UploadAsVersionableByDefault;
                    uploadArgs.Language = language;
                    uploadArgs.CloseDialogOnEnd = false;
                    PipelineFactory.GetPipeline("uiUpload").Start(uploadArgs);
                    string fileName;
                    if (uploadArgs.UploadedItems.Count > 0)
                    {
                        fileName = uploadArgs.UploadedItems[0].ID.ToString();
                        PowerShellLog.Audit("Upload: {0}", StringUtil.Join(uploadArgs.UploadedItems, ", ", "Name"));
                    }
                    else
                    {
                        var fileHandle = uploadArgs.Properties["filename"];
                        if (fileHandle != null)
                        {
                            fileName = WebUtil.UrlEncode(FileHandle.GetFilename(fileHandle.ToString()));
                        }
                        else if (uploadArgs.Unpack)
                        {
                            fileName = WebUtil.UrlEncode(uploadArgs.Folder);
                        }
                        else
                        {
                            fileName = string.Empty;
                        }
                    }
                    if (!string.IsNullOrEmpty(uploadArgs.ErrorText))
                        return;
                    HttpContext.Current.Response.Write(
                        "<html><head><script type=\"text/JavaScript\" language=\"javascript\">window.top.scForm.getTopModalDialog().frames[0].scForm.postRequest(\"\", \"\", \"\", 'EndUploading(\"" +
                        fileName + "\")')</script></head><body>Done</body></html>");
                }
                catch (OutOfMemoryException)
                {
                    HttpContext.Current.Response.Write(
                        "<html><head><script type=\"text/JavaScript\" language=\"javascript\">window.top.scForm.getTopModalDialog().frames[0].scForm.postRequest(\"\", \"\", \"\", 'ShowFileTooBig(" +
                        StringUtil.EscapeJavascriptString(Request.Files[0].FileName) +
                        ")')</script></head><body>Done</body></html>");
                }
                catch (Exception ex)
                {
                    if (ex.InnerException is OutOfMemoryException)
                        HttpContext.Current.Response.Write(
                            "<html><head><script type=\"text/JavaScript\" language=\"javascript\">window.top.scForm.getTopModalDialog().frames[0].scForm.postRequest(\"\", \"\", \"\", 'ShowFileTooBig(" +
                            StringUtil.EscapeJavascriptString(Request.Files[0].FileName) +
                            ")')</script></head><body>Done</body></html>");
                    else
                        HttpContext.Current.Response.Write(
                            "<html><head><script type=\"text/JavaScript\" language=\"javascript\">window.top.scForm.getTopModalDialog().frames[0].scForm.postRequest(\"\", \"\", \"\", 'ShowError')</script></head><body>Done</body></html>");
                }
            }
        }
    }
}