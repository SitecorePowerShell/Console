using System;
using System.IO;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Resources.Media;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.SitecoreIntegrations.Applications
{
    public class DownloadFile : BaseForm
    {
        protected Border Buttons;
        protected Literal Text;
        protected string FileName { get; set; }
        protected string Id { get; set; }
        protected string Db { get; set; }
        protected Edit Hidden;
        protected Literal FileNameLabel;
        protected Literal SizeLabel;

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);

            FileName = WebUtil.SafeEncode(WebUtil.GetQueryString("fn"));

            Id = WebUtil.SafeEncode(WebUtil.GetQueryString("id"));
            Db = WebUtil.SafeEncode(WebUtil.GetQueryString("db"));

            Context.ClientPage.ClientResponse.SetDialogValue(Hidden.Value);
            if (Context.ClientPage.IsEvent)
                return;
            Text.Text = WebUtil.SafeEncode(WebUtil.GetQueryString("te"));

            if (!string.IsNullOrEmpty(Id))
            {
                Item item = Factory.GetDatabase(Db).GetItem(new ID(Id));
                if (MediaManager.HasMediaContent(item))
                {
                    Media media = MediaManager.GetMedia(item);
                    FileNameLabel.Text = item.Name + "." + item["Extension"];
                    long size;
                    SizeLabel.Text = Int64.TryParse(item["size"], out size) ? ToFileSize(size) : "unknown";
                }
                else
                {
                    SheerResponse.Alert("There is no file attached.", new string[0]);
                }
            }
            else if (!string.IsNullOrEmpty(FileName))
            {
                FileNameLabel.Text = FileName;
                SheerResponse.Download(FileName);
                Hidden.Value = "downloaded";
                var file = new FileInfo(FileName);
                SizeLabel.Text = ToFileSize(file.Length);
            }

            string caption = WebUtil.SafeEncode(WebUtil.GetQueryString("cp"));
            Context.ClientPage.Title = caption;
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            Text.Text = WebUtil.SafeEncode(WebUtil.GetQueryString("te"));
            Hidden.Value = "cancelled";
            Context.ClientPage.ClientResponse.SetDialogValue(Hidden.Value);
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

        /// <summary>
        ///     Closes this dialog. Dialog value will be 'no'.
        /// </summary>
        protected void Close()
        {
            Context.ClientPage.ClientResponse.SetDialogValue(Hidden.Value);
            Context.ClientPage.ClientResponse.CloseWindow();
        }

        /// <summary>
        ///     Closes this dialog. Dialog value will be 'no'.
        /// </summary>
        protected void Download()
        {
            if (!string.IsNullOrEmpty(Id))
            {
                Item item = Factory.GetDatabase(Db).GetItem(new ID(Id));
                if (MediaManager.HasMediaContent(item))
                {
                    UrlString str = item.Uri.ToUrlString(string.Empty);
                    str.Append("field", "Blob");
                    Files.Download(str.ToString());
                    Log.Audit(this, "Download file: {0}", new[] {str.ToString()});
                }
                else
                {
                    SheerResponse.Alert("There is no file attached.", new string[0]);
                }
            }
            else if (!string.IsNullOrEmpty(FileName))
            {
                SheerResponse.Download(FileName);
                Hidden.Value = "downloaded";
            }
        }
    }
}