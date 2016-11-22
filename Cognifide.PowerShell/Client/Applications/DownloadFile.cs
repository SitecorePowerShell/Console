using System;
using System.IO;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Settings.Authorization;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Resources.Media;
using Sitecore.Shell.Framework;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Applications
{
    public class DownloadFile : DialogForm
    {
        protected Border Buttons;
        protected Literal FileNameLabel;
        protected Literal PathPrefix;
        protected Literal SizePrefix;
        protected ThemedImage DownloadImage;
        protected ThemedImage ErrorImage;
        protected Edit Hidden;
        protected Literal SizeLabel;
        protected Literal Text;

        public static string FileName
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["FileName"]); }
            set { Context.ClientPage.ServerProperties["FileName"] = value; }
        }

        public static string ItemUri
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ItemUri"]); }
            set { Context.ClientPage.ServerProperties["ItemUri"] = value; }
        }

        public static string ItemDb
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ItemDb"]); }
            set { Context.ClientPage.ServerProperties["ItemDb"] = value; }
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            Assert.IsTrue(ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceExecution, Context.User.Name, false), "Application access denied.");
            base.OnLoad(e);

            Context.ClientPage.ClientResponse.SetDialogValue(Hidden.Value);
            if (Context.ClientPage.IsEvent)
                return;

            UrlHandle handle = null;
            if (!UrlHandle.TryGetHandle(out handle))
            {
                FileNameLabel.Text =
                    "Invalid dialog invocation.";
                SizeLabel.Visible=false;
                PathPrefix.Visible = false;
                SizePrefix.Visible = false;
                OK.Visible = false;
                DownloadImage.Visible = false;
                ErrorImage.Visible = true;
                return;
            }

            FileName = handle["fn"];

            ItemUri = handle["uri"];
            ItemDb = handle["db"];

            bool showFullPath;
            if (!bool.TryParse(handle["fp"], out showFullPath))
            {
                showFullPath = false;
            }

            Text.Text = handle["te"];

            if (!string.IsNullOrEmpty(ItemUri))
            {
                var item = Factory.GetDatabase(ItemDb).GetItem(new DataUri(ItemUri));
                if (MediaManager.HasMediaContent(item))
                {
                    FileNameLabel.Text = (showFullPath ? item.GetProviderPath() : item.Name)
                                         + "." + item["Extension"];
                    long size;
                    SizeLabel.Text = Int64.TryParse(item["size"], out size) ? ToFileSize(size) : "unknown";
                }
                else
                {
                    SheerResponse.Alert("There is no file attached.");
                }
            }
            else if (!string.IsNullOrEmpty(FileName))
            {
                // check if file in approved location
                var filePath = FileUtil.MapPath(FileName);
                var webSitePath = FileUtil.MapPath("/");
                var dataPath = FileUtil.MapPath(Settings.DataFolder);

                if (!filePath.StartsWith(webSitePath, StringComparison.InvariantCultureIgnoreCase) &&
                    !filePath.StartsWith(dataPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    FileNameLabel.Text =
                        "Files from outside of the Sitecore Data and Website folders cannot be downloaded.\n\n" +
                        "Copy the file to the Sitecore Data folder and try again.";
                    SizeLabel.Visible = false;
                    PathPrefix.Visible = false;
                    SizePrefix.Visible = false;
                    OK.Visible = false;
                    DownloadImage.Visible = false;
                    ErrorImage.Visible = true;
                    return;
                }

                FileNameLabel.Text = showFullPath ? FileName : Path.GetFileName(FileName);
                SheerResponse.Download(FileName);
                Hidden.Value = "downloaded";
                var file = new FileInfo(FileName);
                SizeLabel.Text = ToFileSize(file.Length);
            }

            var caption = handle["cp"];
            Context.ClientPage.Title = caption;
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            Text.Text = handle["te"];
            Hidden.Value = "cancelled";
            Context.ClientPage.ClientResponse.SetDialogValue(Hidden.Value);
            UrlHandle.DisposeHandle(handle);
        }

        public static string ToFileSize(long size)
        {
            if (size < 1024)
            {
                return (size).ToString("F0") + " bytes";
            }
            if (size < Math.Pow(1024, 2))
            {
                return (size/1024).ToString("F0") + " KB";
            }
            if (size < Math.Pow(1024, 3))
            {
                return (size/Math.Pow(1024, 2)).ToString("F0") + " MB";
            }
            if (size < Math.Pow(1024, 4))
            {
                return (size/Math.Pow(1024, 3)).ToString("F0") + " GB";
            }
            if (size < Math.Pow(1024, 5))
            {
                return (size/Math.Pow(1024, 4)).ToString("F0") + " TB";
            }
            if (size < Math.Pow(1024, 6))
            {
                return (size/Math.Pow(1024, 5)).ToString("F0") + " PB";
            }
            return (size/Math.Pow(1024, 6)).ToString("F0") + " EB";
        }

        protected void Close()
        {
            Context.ClientPage.ClientResponse.SetDialogValue(Hidden.Value);
            Context.ClientPage.ClientResponse.CloseWindow();
        }

        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            if (!string.IsNullOrEmpty(ItemUri))
            {
                var item = Factory.GetDatabase(ItemDb).GetItem(new DataUri(ItemUri));
                if (MediaManager.HasMediaContent(item))
                {
                    var str = item.Uri.ToUrlString(string.Empty);
                    str.Append("field", "Blob");
                    Files.Download(str.ToString());
                    PowerShellLog.Audit("Download file: {0}", str.ToString());
                }
                else
                {
                    SheerResponse.Alert("There is no file attached.");
                }
            }
            else if (!string.IsNullOrEmpty(FileName))
            {
                SheerResponse.Download(FileName);
                Hidden.Value = "downloaded";
            }
            Hidden.Value = "downloaded";
            Context.ClientPage.ClientResponse.SetDialogValue(Hidden.Value);
            //base.OnCancel(sender, args);
        }
    }
}