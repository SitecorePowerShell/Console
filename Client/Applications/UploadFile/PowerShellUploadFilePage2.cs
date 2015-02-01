using System;
using System.Web;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
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
                    var language = Sitecore.Context.ContentLanguage;
                    var itemUri = ItemUri.Parse(pathOrId);
                    var uploadArgs = new UploadArgs();
                    if (itemUri != null)
                    {
                        pathOrId = itemUri.GetPathOrId();
                        language = itemUri.Language;
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
                    uploadArgs.Overwrite = Settings.Upload.SimpleUploadOverwriting;
                    uploadArgs.Unpack = false;
                    uploadArgs.Versioned = Settings.Media.UploadAsVersionableByDefault;
                    uploadArgs.Language = language;
                    uploadArgs.CloseDialogOnEnd = false;
                    PipelineFactory.GetPipeline("uiUpload").Start(uploadArgs);
                    string fileName;
                    if (uploadArgs.UploadedItems.Count > 0)
                    {
                        fileName = uploadArgs.UploadedItems[0].ID.ToString();
                        Log.Audit(this, "Upload: {0}", StringUtil.Join(uploadArgs.UploadedItems, ", ", "Name"));
                    }
                    else
                    {
                        object fileHandle = uploadArgs.Properties["filename"];
                        if (fileHandle != null)
                        {
                            fileName = WebUtil.UrlEncode(FileHandle.GetFilename(fileHandle.ToString()));
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
                catch (OutOfMemoryException ex)
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