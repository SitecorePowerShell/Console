using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Provider;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets
{
    public class BaseCommand : PSCmdlet
    {
        protected bool IsCurrentDriveSitecore
        {
            get
            {
                return SessionState.Drive.Current.Provider.ImplementingType == typeof (PsSitecoreItemProvider) ||
                       SessionState.Drive.Current.Provider.ImplementingType.IsSubclassOf(typeof (PsSitecoreItemProvider));
            }
        }

        protected ProviderInfo Provider
        {
            get { return SessionState.Drive.Current.Provider; }
        }

        protected string CurrentDrive
        {
            get { return SessionState.Drive.Current.Name; }
        }

        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        protected Database CurrentDatabase
        {
            get
            {
                if (IsCurrentDriveSitecore)
                {
                    return Factory.GetDatabase(CurrentDrive);
                }
                WriteError(new ErrorRecord(
                    new InvalidPowerShellStateException(
                        "Current Sitecore database cannot be established, current location is not within a Sitecore content tree.")
                    , "location_not_in_sitecore_database", ErrorCategory.DeviceError, null));
                return null;
            }
        }

        protected ScriptingHostPrivateData HostData { get { return (Host.PrivateData.BaseObject as ScriptingHostPrivateData); } }

        protected string CurrentPath
        {
            get { return SessionState.Drive.Current.CurrentLocation; }
        }

        protected ClientPage ClientPage
        {
            get { return (GetVariableValue("ClientPage") as ClientPage); }
        }

        protected void RecoverHttpContext()
        {
            HttpContext.Current = SessionState.PSVariable.Get("HttpContext").Value as HttpContext;
        }

        protected static WildcardPattern GetWildcardPattern(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                name = "*";
            }
            const WildcardOptions options = WildcardOptions.IgnoreCase | WildcardOptions.Compiled;
            var wildcard = new WildcardPattern(name, options);
            return wildcard;
        }

        protected void WildcardWrite<T>(string filter, IEnumerable<T> items, Func<T, string> propertyName)
        {
            WriteObject(WildcardFilter(filter, items, propertyName), true);
        }

        protected static IEnumerable<T> WildcardFilter<T>(string filter, IEnumerable<T> items,
            Func<T, string> propertyName)
        {
            WildcardPattern wildcardPattern = GetWildcardPattern(filter);
            return items.Where(item => wildcardPattern.IsMatch(propertyName(item)));
        }

        protected static IEnumerable<T> WildcardFilterMany<T>(string[] filters, IEnumerable<T> items,
            Func<T, string> propertyName)
        {
            var matchingItems = new Dictionary<string, T>();
            var itemsList = items.ToList();
            foreach (var filter in filters)
            {
                foreach (T matchingItem in WildcardFilter(filter, itemsList, propertyName))
                {
                    matchingItems[propertyName(matchingItem)] = matchingItem;
                }
            }
            return matchingItems.Values;
        }

        protected virtual Item FindItemFromParameters(Item item, string path, string id)
        {
            if (item == null)
            {
                if (!String.IsNullOrEmpty(id))
                {
                    Database currentDb = Factory.GetDatabase(CurrentDrive);
                    item = currentDb.GetItem(new ID(id));
                }
                else if (!String.IsNullOrEmpty(path))
                {
                    path = path.Replace('\\', '/');
                    item = PathUtilities.GetItem(path, CurrentDrive, CurrentPath);
                }
                else
                {
                    WriteError(new ErrorRecord(
                        new ObjectNotFoundException("Cannot find item to perform the operation on."),
                        "sitecore_item_not_found", ErrorCategory.ObjectNotFound, null
                        ));
                }
            }
            return item;
        }

        protected virtual Database FindDatabaseFromParameters(Item item, string path, Database database)
        {
            if (item == null)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return database ?? CurrentDatabase;
                }

                int driveSeparator = path.IndexOf(':');
                if (driveSeparator > 0)
                {
                    string driveName = path.Substring(0, driveSeparator);
                    return Factory.GetDatabase(driveName);
                }
                return CurrentDatabase;
            }
            return item.Database;
        }

        protected virtual Item FindItemFromParameters(Item item, string path, string id, Language language, Database database)
        {
            if (item == null)
            {
                if (!String.IsNullOrEmpty(id))
                {
                    Database currentDb = database ?? CurrentDatabase;
                    item = currentDb.GetItem(new ID(id));
                }
                else if (!String.IsNullOrEmpty(path))
                {
                    path = path.Replace('\\', '/');
                    item = PathUtilities.GetItem(path, CurrentDrive, CurrentPath);
                }
                else
                {
                    WriteError(new ErrorRecord(
                        new ObjectNotFoundException("Cannot find item to perform the operation on."),
                        "sitecore_item_not_found", ErrorCategory.ObjectNotFound, null
                        ));
                }
                if (item != null)
                {
                    item = language != null ? item.Versions.GetLatestVersion(language) : item.Versions.GetLatestVersion();
                }
            }
            return item;
        }

        protected void LogErrors(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log.Error("Error while executing '{0}' command", ex, this);
                WriteError(new ErrorRecord(ex,"loggable_error",ErrorCategory.NotSpecified, null));
            }
        }

        protected void WriteItem(Item item)
        {
            // add the properties defined by the page type
            PSObject psobj = ItemShellExtensions.GetPsObject(SessionState, item);
            WriteObject(psobj);
        }
    }
}