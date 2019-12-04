using System;
using System.Collections;
using Sitecore;
using Sitecore.Shell.Applications.Install;
using Sitecore.Text;
using Sitecore.Web;

namespace Spe.Commands.Interactive.Messages
{
    [Serializable]
    public class ShowModalDialogPsMessage : BasePipelineMessageWithResult
    {

        public ShowModalDialogPsMessage(string url, string width, string height, Hashtable handleParams)
        {
            Width = width ?? string.Empty;
            Height = height ?? string.Empty;
            HandleParams = handleParams;
            ReceiveResults = true;
            Url = url;
        }


        public string Width { get; }
        public string Height { get; }
        public string Title { get; set; }
        public string Url { get; }
        public Hashtable HandleParams { get; }
        public bool ReceiveResults { get; set; }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            var urlString = new UrlString(Url);
            if (HandleParams != null && HandleParams.Count > 0)
            {
                var handle = new UrlHandle();
                foreach (string key in HandleParams.Keys)
                {
                    var value = HandleParams[key];
                    if (value is string strValue &&
                        strValue.StartsWith("packPath:", StringComparison.OrdinalIgnoreCase))
                    {
                        strValue = strValue.Substring(9);
                        handle[key] = ApplicationContext.StoreObject(strValue);
                    }
                    else
                    {
                        handle[key] = value?.ToString() ?? string.Empty;
                    }
                }
                handle.Add(urlString);
            }

            Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), Width, Height, string.Empty, ReceiveResults);
        }

        protected override object ProcessResult(bool hasResult, string result)
        {
            result = hasResult ? result : null;
            if (string.IsNullOrEmpty(result))
            {
                result = "undetermined";
            }
            return result;
        }
    }
}