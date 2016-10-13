using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Web;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Provider;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets
{
    public class BaseCommand : PSCmdlet
    {
        protected bool IsCurrentDriveSitecore => SessionState.Drive.Current.Provider.ImplementingType == typeof (PsSitecoreItemProvider) ||
                                                 SessionState.Drive.Current.Provider.ImplementingType.IsSubclassOf(typeof (PsSitecoreItemProvider));

        protected ProviderInfo Provider => SessionState.Drive.Current.Provider;

        protected string CurrentDrive => SessionState.Drive.Current.Name;

        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        protected Database CurrentDatabase
        {
            get
            {
                if (IsCurrentDriveSitecore)
                {
                    return Factory.GetDatabase(CurrentDrive);
                }
                WriteError(typeof(InvalidPowerShellStateException),
                    "Current Sitecore database cannot be established, current location is not within a Sitecore content tree.",
                    ErrorIds.DatabaseNotFound, ErrorCategory.DeviceError, null, true);
                return null;
            }
        }

        protected ScriptingHostPrivateData HostData => (Host.PrivateData.BaseObject as ScriptingHostPrivateData);
        protected bool InteractiveSession => HostData.Interactive;

        protected string CurrentPath => SessionState.Drive.Current.CurrentLocation;

        protected ClientPage ClientPage => (GetVariableValue("ClientPage") as ClientPage);

        protected void RecoverHttpContext()
        {
            var job = Context.Job;
            HttpContext.Current = SessionState.PSVariable.Get("HttpContext").Value as HttpContext;
            Context.Job = job;
        }

        protected void WildcardWrite<T>(string filter, IEnumerable<T> items, Func<T, string> propertyName)
        {
            WriteObject(WildcardFilter(filter, items, propertyName), true);
        }

        protected static IEnumerable<T> WildcardFilter<T>(string filter, IEnumerable<T> items,
            Func<T, string> propertyName)
        {
            return WildcardUtils.WildcardFilter(filter, items, propertyName);
        }

        protected static IEnumerable<T> WildcardFilterMany<T>(string[] filters, IEnumerable<T> items,
            Func<T, string> propertyName)
        {
            return WildcardUtils.WildcardFilterMany(filters, items, propertyName);
        }

        protected virtual Item FindItemFromParameters(Item item, string path, string id)
        {
            if (item != null) return item;

            if (!String.IsNullOrEmpty(id))
            {
                var currentDb = Factory.GetDatabase(CurrentDrive);
                item = currentDb.GetItem(new ID(id));
            }
            else if (!String.IsNullOrEmpty(path))
            {
                path = path.Replace('\\', '/');
                item = PathUtilities.GetItem(path, CurrentDrive, CurrentPath);
            }
            else
            {
                WriteError(typeof(ObjectNotFoundException), "Cannot find item to perform the operation on.",
                    ErrorIds.ItemNotFound, ErrorCategory.ObjectNotFound, null);
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

                var driveSeparator = path.IndexOf(':');
                if (driveSeparator <= 0) return CurrentDatabase;

                var driveName = path.Substring(0, driveSeparator);
                return Factory.GetDatabase(driveName);
            }
            return item.Database;
        }

        protected virtual Item FindItemFromParameters(Item item, string path, string id, Language language,
            string databaseName)
        {
            if (item != null) return item;

            if (!string.IsNullOrEmpty(id))
            {
                var currentDb = String.IsNullOrEmpty(databaseName) ? CurrentDatabase : Factory.GetDatabase(databaseName);
                if (currentDb != null)
                {
                    item = currentDb.GetItem(new ID(id));
                }
            }
            else if (!string.IsNullOrEmpty(path))
            {
                path = path.Replace('\\', '/');
                item = PathUtilities.GetItem(path, CurrentDrive, CurrentPath);
            }
            else
            {
                WriteError(typeof(ObjectNotFoundException), "Cannot find item to perform the operation on.",
                    ErrorIds.ItemNotFound, ErrorCategory.ObjectNotFound, null);
            }
            if (item != null)
            {
                item = language != null
                    ? item.Versions.GetLatestVersion(language)
                    : item.Versions.GetLatestVersion();
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
                PowerShellLog.Error($"Error while executing '{GetType().FullName}' command", ex);
                WriteError(ex.GetType(), $"An error was encountered while executing the command '{this}'", ErrorIds.InvalidOperation, ErrorCategory.NotSpecified, null);
            }
        }

        protected void WriteItem(Item item)
        {
            if (item == null) return;

            // add the properties defined by the page type
            var psobj = ItemShellExtensions.GetPsObject(SessionState, item);
            WriteObject(psobj);
        }

        protected bool IsParameterSpecified(string name)
        {
            return MyInvocation.BoundParameters.ContainsKey(name);
        }

        protected static string WrapNameWithSpacesInQuotes(string name)
        {
            return name.Contains(" ") ? "\"" + name + "\"" : name;
        }

        protected virtual void WriteError(Type exceptionType, string error, ErrorIds errorIds, ErrorCategory errorCategory, object targetObject, bool throwTerminatingError = false)
        {
            var exceptionInstance = (Exception)Activator.CreateInstance(exceptionType, error);
            WriteError(exceptionInstance, errorIds, errorCategory, targetObject, throwTerminatingError);
        }

        protected virtual void WriteError(Exception exception, ErrorIds errorIds, ErrorCategory errorCategory, object targetObject, bool throwTerminatingError = false)
        {
            var record = new ErrorRecord(exception, errorIds.ToString(), errorCategory, targetObject);
            PowerShellLog.Error($"'{errorIds}' (Category: {errorCategory}) error encountered on object. ", exception);
            if (throwTerminatingError) ThrowTerminatingError(record);
            WriteError(record);
        }

        protected virtual bool CheckSessionCanDoInteractiveAction()
        {
            if (InteractiveSession) return InteractiveSession;

            CmdletAttribute attribute = GetType().GetCustomAttributes(typeof (CmdletAttribute), true).FirstOrDefault() as CmdletAttribute;
            var message = attribute == null
                ? "Non interactive session cannot perform an interactive operation."
                : $"Non interactive session cannot perform an interactive operation requested by the '{attribute.VerbName}-{attribute.NounName}' command.";
            WriteError(typeof (InvalidOperationException), message,
                ErrorIds.ScriptSessionIsNotInteractive, ErrorCategory.InvalidOperation, this);
            return InteractiveSession;
        }
    }
}