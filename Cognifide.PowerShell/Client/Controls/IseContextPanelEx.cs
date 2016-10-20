using System;
using Cognifide.PowerShell.Client.Applications;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Client.Controls
{
    public class IseContextPanelEx : IseContextPanelBase
    {
        protected override Item Button1 => Factory.GetDatabase("core").GetItem("{3939B85B-3A9B-45FA-8EFB-67AF84E0CF18}");
        protected override Item Button2 => Factory.GetDatabase("core").GetItem("{71044CE9-03D1-4D5C-81B5-854993686189}");
        protected override string Label1 
        {
            get
            {
                var lang = CurrentLanguage;
                return string.Equals(lang.Name, Context.Language.Name, StringComparison.OrdinalIgnoreCase)
                    ? $"Current Language [{lang.Name}]"
                    : $"{lang.CultureInfo.EnglishName} [{lang.Name}]";
            }
        }

        protected override string Icon1 => CurrentLanguage.GetIcon();

        protected override string Label2
        {
            get
            {
                var user = CurrentUser;
                return string.Equals(user.Name, Context.User.Name, StringComparison.OrdinalIgnoreCase)
                    ? $"{user.Name} [Current]"
                    : user.Name;
            }
        }

        protected override string Icon2 => CurrentUser.Profile.Portrait;

        Language CurrentLanguage => CommandContext.Parameters["currentLanguage"] == PowerShellIse.DefaultLanguage
            ? Context.Language
            : LanguageManager.GetLanguage(CommandContext.Parameters["currentLanguage"]);

        User CurrentUser => CommandContext.Parameters["currentUser"] == PowerShellIse.DefaultUser
            ? Context.User
            : User.FromName(CommandContext.Parameters["currentUser"], true);
    }
}