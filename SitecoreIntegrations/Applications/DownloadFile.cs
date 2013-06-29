using System;
using System.IO;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Diagnostics;
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
        protected Edit Hidden;
        protected Literal FileNameLabel;
        protected Literal SizeLabel;

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            FileName = WebUtil.SafeEncode(WebUtil.GetQueryString("fn"));
            Context.ClientPage.ClientResponse.SetDialogValue(Hidden.Value);
            if (Context.ClientPage.IsEvent)
                return;
            Text.Text = WebUtil.SafeEncode(WebUtil.GetQueryString("te"));
            FileNameLabel.Text = FileName;
            var file = new FileInfo(FileName);
            SizeLabel.Text = ToFileSize(file.Length);
            string caption = WebUtil.SafeEncode(WebUtil.GetQueryString("cp"));
            Context.ClientPage.Title = caption;
            Assert.ArgumentNotNull((object)e, "e");
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
            else if (size < Math.Pow(1024, 2))
            {
                return (size / 1024).ToString("F0") + " KB";
            }
            else if (size < Math.Pow(1024, 3))
            {
                return (size / Math.Pow(1024, 2)).ToString("F0") + " MB";
            }
            else if (size < Math.Pow(1024, 4))
            {
                return (size / Math.Pow(1024, 3)).ToString("F0") + " GB";
            }
            else if (size < Math.Pow(1024, 5))
            {
                return (size / Math.Pow(1024, 4)).ToString("F0") + " TB";
            }
            else if (size < Math.Pow(1024, 6))
            {
                return (size / Math.Pow(1024, 5)).ToString("F0") + " PB";
            }
            else
            {
                return (size / Math.Pow(1024, 6)).ToString("F0") + " EB";
            }
        }

        /// <summary>
        /// Closes this dialog. Dialog value will be 'no'.
        /// </summary>
        protected void Close()
        {
            Context.ClientPage.ClientResponse.SetDialogValue(Hidden.Value);
            Context.ClientPage.ClientResponse.CloseWindow();
        }

        /// <summary>
        /// Closes this dialog. Dialog value will be 'no'.
        /// 
        /// </summary>
        protected void Download()
        {
            SheerResponse.Download(FileName);
            Hidden.Value = "downloaded";
        }

    }
}