using System;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Resources;
using Sitecore.Shell.Applications.ContentManager.Galleries;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.XmlControls;

namespace Cognifide.PowerShell.Client.Controls
{
    public class LanguageGallery : GalleryForm
    {
        protected GalleryMenu Options;
        protected Scrollbox Languages;
        private string CurrentLanguage { get; set; }

        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (message.Name != "event:click")
            {
                Invoke(message, true);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                CurrentLanguage = WebUtil.GetQueryString("currentLanguage");
                var addedLanguages = new Set<string>();

                foreach (var language in new LanguageHistory().GetLanguages())
                {
                    AddLanguage(language, addedLanguages);
                }
                foreach (var language in LanguageManager.GetLanguages(Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb)))
                {
                    AddLanguage(language, addedLanguages);
                }
                var item =
                    Sitecore.Client.CoreDatabase.GetItem(
                        "/sitecore/content/Applications/PowerShell/PowerShellIse/Menus/Languages");
                if ((item != null) && item.HasChildren)
                {
                    var queryString = WebUtil.GetQueryString("id");
                    var commandContext = new CommandContext(item);
                    commandContext.Parameters.Add("language", CurrentLanguage);
                    Options.AddFromDataSource(item, queryString, commandContext);
                }
            }
        }

        private void AddLanguage(Language language, Set<string> addedLanguages)
        {
            if (language == null || addedLanguages.Contains(language.Name))
            {
                return;
            }

            addedLanguages.Add(language.Name);
            var control = ControlFactory.GetControl("LanguageGallery.Option") as XmlControl;
            Assert.IsNotNull(control, "Xml Control \"LanguageGallery.Option\" not found");
            Context.ClientPage.AddControl(Languages, control);

            var icon = language.GetIcon();
            //var icon = CurrentVersion.IsAtLeast(SitecoreVersion.V80) ? "Office/32x32/flag_generic.png" : "Flags/32x32/flag_generic.png";

            var builder = new ImageBuilder
            {
                Src = Images.GetThemedImageSource(icon, ImageDimension.id16x16),
                Class = "scRibbonToolbarSmallGalleryButtonIcon",
                Alt = language.Name
            };

            control["Class"] = (language.Name == CurrentLanguage) ? "selected" : string.Empty;
            control["LanguageIcon"] = $"<div class=\"versionNum\">{builder}</div>";
            control["LanguageCode"] = Translate.Text("<b>{0}</b>", language.Name);
            control["Name"] = Translate.Text("<b>{0}</b>.", language.GetDisplayName());
            control["Click"] = $"ise:setlanguage(language={language.Name})";
        }
    }
}