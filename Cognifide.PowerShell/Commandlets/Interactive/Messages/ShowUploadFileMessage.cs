using System;
using Sitecore;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class ShowUploadFileMessage : BasePipelineMessageWithResult
    {

        public ShowUploadFileMessage(string width, string height, string title, string description, string okButtonName,
            string cancelButtonName, string path, bool versioned, string language, bool overwrite, bool unpack,
            bool advancedDialog)
        {
            Width = width ?? string.Empty;
            Height = height ?? string.Empty;
            Title = title ?? string.Empty;
            OkButtonName = okButtonName ?? string.Empty;
            CancelButtonName = cancelButtonName ?? string.Empty;
            Description = description ?? string.Empty;
            Path = path;
            Versioned = versioned;
            Language = language;
            Overwrite = overwrite;
            Unpack = unpack;
            AdvancedDialog = advancedDialog;
        }

        public bool AdvancedDialog { get; set; }
        public string Width { get; private set; }
        public string Height { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string CancelButtonName { get; private set; }
        public string OkButtonName { get; private set; }
        public string Path { get; private set; }
        public bool Unpack { get; set; }
        public bool Overwrite { get; set; }
        public string Language { get; set; }
        public bool Versioned { get; set; }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            if (AdvancedDialog)
            {
                var urlString =
                    new UrlString("/sitecore/shell/Applications/Media/UploadManager/UploadManager.aspx");
                var item = Context.ContentDatabase.GetItem(Path);
                item.Uri.AddToUrlString(urlString);
                SheerResponse.ShowModalDialog(urlString.ToString(), true);
            }
            else
            {
                var urlString = new UrlString("/sitecore modules/Shell/PowerShell/UploadFile/PowerShellUploadFile.aspx");
                var handle = new UrlHandle();
                handle["te"] = Title ?? string.Empty;
                handle["ds"] = Description ?? string.Empty;
                handle["ic"] = "powershell/32x32/powershell8.png";
                handle["ok"] = OkButtonName ?? string.Empty;
                handle["cancel"] = CancelButtonName ?? string.Empty;
                handle["path"] = Path;
                handle["mask"] = "*.*";
                handle["de"] = "txt";
                handle["versioned"] = Versioned ? "1" : string.Empty;
                handle["language"] = Language ?? string.Empty;
                handle["overwrite"] = Overwrite ? "1" : string.Empty;
                handle["unpack"] = Unpack ? "1" : string.Empty;

                handle.Add(urlString);
                SheerResponse.ShowModalDialog(urlString.ToString(), Width, Height, "", true);
            }
        }

        protected override object ProcessResult(bool hasResult, string result)
        {
            result = hasResult ? result : null;
            if (string.IsNullOrEmpty(result) && AdvancedDialog)
            {
                result = "undetermined";
            }
            return result;
        }
    }
}