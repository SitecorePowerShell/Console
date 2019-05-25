using Sitecore.Shell.Applications.ContentEditor;

namespace Spe.Client.Controls
{
    public class PSCheckList : Checklist
    {
        public void SetItemLanguage(string languageName)
        {
            ViewState["ItemLanguage"] = languageName;
        }
    }
}