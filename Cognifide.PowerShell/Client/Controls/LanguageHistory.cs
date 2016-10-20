using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;

namespace Cognifide.PowerShell.Client.Controls
{
    public class LanguageHistory
    {
        private readonly ListString _list = new ListString(Registry.GetString(HistoryKey));
        private const string HistoryKey = "/Current_Users/RecentLanguageList";
        private const int Historylength = 8;

        // Methods
        public void Add(string languageName)
        {
            Assert.ArgumentNotNullOrEmpty(languageName, "languageName");
            for (var i = _list.Count - 1; i >= 0; i--)
            {
                if (string.Compare(_list[i], languageName, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    _list.Remove(i);
                }
            }
            _list.AddAt(0, languageName);
            while (_list.Count > Historylength)
            {
                _list.Remove(_list.Count - 1);
            }
            Save();
        }

        public IEnumerable<Language> GetLanguages()
        {
            return _list.Select(LanguageManager.GetLanguage);
        }

        private void Save()
        {
            Registry.SetString(HistoryKey, _list.ToString());
        }
    }
}