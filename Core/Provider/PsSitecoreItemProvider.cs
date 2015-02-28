using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Web;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Proxies;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Exceptions;
using Version = Sitecore.Data.Version;

namespace Cognifide.PowerShell.Core.Provider
{
    [CmdletProvider("PsSitecoreItemProvider",
        ProviderCapabilities.Filter | ProviderCapabilities.ShouldProcess | ProviderCapabilities.ExpandWildcards)]
    [OutputType(typeof (Item), ProviderCmdlet = "Get-ChildItem")]
    [OutputType(typeof (Item), ProviderCmdlet = "Get-Item")]
    [OutputType(typeof (Item), ProviderCmdlet = "New-Item")]
    [OutputType(typeof (Item), ProviderCmdlet = "Copy-Item")]
    public partial class PsSitecoreItemProvider : NavigationCmdletProvider, IPropertyCmdletProvider
    {
        private ProviderInfo providerInfo;

        /// <summary>
        ///     Moves page to recycle bin on remove-item
        /// </summary>
        /// <param name="path"></param>
        /// <param name="recurse"></param>
        protected override void RemoveItem(string path, bool recurse)
        {
            try
            {
                LogInfo("Executing ConvertPath(string path='{0}', string recurse='{1}')", path, recurse);
                var item = GetItemForPath(path);
                if (item != null)
                {
                    CheckOperationAllowed("remove", item.Access.CanDelete(), item.Uri.ToString());
                    if (ShouldProcess(item.Paths.Path))
                    {
                        if (IsDynamicParamSet(PermanentlyParam))
                        {
                            item.Delete();
                        }
                        else
                        {
                            item.Recycle();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex,
                    "Error while executing ConvertPath(string path='{0}', string recurse='{1}')",
                    path, recurse);
                throw;
            }
        }

        protected override bool HasChildItems(string path)
        {
            try
            {
                LogInfo("Executing HasChildItems(string path='{0}')", path);
                path = path.Replace("\\", "/");
                var item = GetItemForPath(path);
                if (item != null)
                {
                    return item.HasChildren;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error while executing HasChildItems(string path='{0}')", path);
                throw;
            }
        }

        protected override void GetChildItems(string path, bool recurse)
        {
            try
            {
                LogInfo("Executing GetChildItems(string path='{0}', string recurse='{1}')", path, recurse);
                var wildcard = new WildcardPattern(Filter ?? "*", WildcardOptions.IgnoreCase | WildcardOptions.Compiled);
                string language;
                int version;
                GetVersionAndLanguageParams(out version, out language);
                GetChildItemsHelper(path, recurse, wildcard, language, version);
            }
            catch (Exception ex)
            {
                LogError(ex,
                    "Error while executing GetChildItems(string path='{0}', string recurse='{1}')",
                    path, recurse);
                throw;
            }
        }

        protected void GetChildItemsHelper(string path, bool recurse, WildcardPattern wildcard, string language,
            int version)

        {
            path = path.Replace("\\", "/");
            if (path.Contains("../"))
            {
                path = path.Substring(path.LastIndexOf("../", StringComparison.Ordinal) + 2);
            }
            var pageRef = GetItemForPath(path);
            //Language lang = LanguageManager.GetLanguage(language);
            if (pageRef != null)
            {
                var children = pageRef.GetChildren();
                foreach (Item childItem in children)
                {
                    var child = childItem;
                    if (wildcard.IsMatch(child.Name))
                    {
                        WriteMatchingItem(language, version, child);
                    }
                    var childUrl = VirtualPathUtility.AppendTrailingSlash(path) + child.Name;

                    // if the specified item exists and recurse has been set then 
                    // all child items within it have to be obtained as well
                    if (ItemExists(path) && recurse)
                    {
                        GetChildItemsHelper(childUrl, true, wildcard, language, version);
                    }
                }
            }
            else
            {
                Exception exception =
                    new IOException(string.Format("Cannot find path '{0}' because it does not exist.", path));
                WriteError(new ErrorRecord(exception, "ItemDoesNotExist", ErrorCategory.ObjectNotFound, path));
            }
        }

        public override string GetResourceString(string baseName, string resourceId)
        {
            return base.GetResourceString(baseName, resourceId);
        }

        protected override string GetChildName(string path)
        {
            var result = base.GetChildName(path);
            LogInfo("Executing GetChildName(string path='{0}'); returns '{1}'", path, result);
            return result;
        }

        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            try
            {
                LogInfo("Executing GetChildNames(string path='{0}', string returnContainers='{1}')", path,
                    returnContainers);
                path = path.Replace("\\", "/");
                if (path.Contains("../"))
                {
                    path = path.Substring(path.LastIndexOf("../", StringComparison.Ordinal) + 2);
                }

                // apply filter
                WildcardPattern wildcard = null;

                if (!string.IsNullOrEmpty(Filter))
                {
                    wildcard = new WildcardPattern(Filter, WildcardOptions.IgnoreCase | WildcardOptions.Compiled);
                }

                var pageRef = GetItemForPath(path);
                if (pageRef != null)
                {
                    var children = pageRef.GetChildren();
                    foreach (Item child in children)
                    {
                        if (returnContainers == ReturnContainers.ReturnAllContainers || wildcard == null ||
                            wildcard.IsMatch(child.Name))
                        {
                            WriteItemObject(child.Name, child.GetProviderPath(), child.HasChildren);
                        }
                        if (Stopping)
                            return;
                    }
                }
                else
                {
                    Exception exception =
                        new IOException(string.Format("Cannot find path '{0}' because it does not exist.", path));
                    WriteError(new ErrorRecord(exception, "ItemDoesNotExist", ErrorCategory.ObjectNotFound, path));
                }
            }
            catch (Exception ex)
            {
                LogError(ex,
                    "Error while executing GetChildNames(string path='{0}', string returnContainers='{1}')",
                    path, returnContainers);
                throw;
            }
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

        protected override void GetItem(string path)
        {
            try
            {
                LogInfo("Executing GetItem(string path='{0}')", path);

                string language;
                int version;

                GetVersionAndLanguageParams(out version, out language);

                var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
                if (dic != null && dic.ContainsKey(UriParam) && dic[UriParam].IsSet)
                {
                    var uri = dic[UriParam].Value.ToString();
                    var itemUri = ItemUri.Parse(uri);
                    var uriItem = Factory.GetDatabase(itemUri.DatabaseName)
                        .GetItem(itemUri.ItemID, itemUri.Language, itemUri.Version);
                    WriteItem(uriItem);
                    return;
                }
                if (dic != null && dic.ContainsKey(QueryParam) && dic[QueryParam].IsSet)
                {
                    var query = dic[QueryParam].Value.ToString();
                    var items = Factory.GetDatabase(PSDriveInfo.Name).SelectItems(query);
                    foreach (var currentItem in items)
                    {
                        WriteMatchingItem(language, version, currentItem);
                    }
                    return;
                }
                if (dic != null && dic.ContainsKey(IdParam) && dic[IdParam].IsSet)
                {
                    var Id = dic[IdParam].Value.ToString();
                    Database database;
                    if (dic.ContainsKey(DatabaseParam) && dic[DatabaseParam].IsSet)
                    {
                        database = (Database) dic[DatabaseParam].Value;
                    }
                    else
                    {
                        database = Factory.GetDatabase(PSDriveInfo.Name);
                    }
                    var itemById = database.GetItem(new ID(Id));
                    if (itemById != null)
                    {
                        WriteMatchingItem(language, version, itemById);
                    }
                    return;
                }


                var item = GetItemForPath(path);
                if (item != null)
                {
                    WriteMatchingItem(language, version, item);
                }
                else
                {
                    ThrowTerminatingInvalidPathException(path);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Error while executing GetItem(string path='{0}')", path);
                throw;
            }
        }

        /// <summary>
        ///     Throws an argument exception stating that the specified path does
        ///     not exist.
        /// </summary>
        /// <param name="path">path which is invalid</param>
        private void ThrowTerminatingInvalidPathException(string path)
        {
            throw new ArgumentException("Path '{0}' does not exist", path);
        }

        protected override void InvokeDefaultAction(string path)
        {
            base.InvokeDefaultAction(path);
        }

        private void WriteMatchingItem(string language, int version, Item item)
        {
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            if (dic != null && dic.ContainsKey(AmbiguousPathsParam) && dic[AmbiguousPathsParam].IsSet)
            {
                var ambiguousItems =
                    item.Parent.GetChildren()
                        .Where(child => string.Equals(child.Name, item.Name, StringComparison.CurrentCultureIgnoreCase));
                foreach (var ambiguousItem in ambiguousItems)
                {
                    WriteMatchingItemEx(language, version, ambiguousItem);
                }
            }
            else
            {
                WriteMatchingItemEx(language, version, item);
            }
        }

        private void WriteMatchingItemEx(string language, int version, Item item)
        {
            // if language is forced get the item in proper language
            if (language != null || version != Version.Latest.Number)
            {
                var pattern = GetWildcardPattern(language);

                var allVersions = item.Versions.GetVersions(!string.IsNullOrEmpty(language));

                foreach (var matchingItem in allVersions.Where(
                    (curItem => (language == null || pattern.IsMatch(curItem.Language.Name)) &&
                                (version == Int32.MaxValue ||
                                 (version == Version.Latest.Number && curItem.Versions.IsLatestVersion()) ||
                                 (version == curItem.Version.Number)
                                    )
                        )))
                {
                    WriteItem(matchingItem);
                }
            }
            else
            {
                WriteItem(item);
            }
        }

        private void WriteItem(Item item)
        {
            // add the properties defined by the page type
            if (item != null)
            {
                var psobj = ItemShellExtensions.GetPsObject(SessionState, item);
                var path = item.Database.Name + ":" + item.Paths.Path.Substring(9).Replace('/', '\\');
                WriteItemObject(psobj, path, item.HasChildren);
            }
        }

        protected override void CopyItem(string path, string destination, bool recurse)
        {
            try
            {
                LogInfo("Executing CopyItem(string path='{0}', string destination='{1}', bool recurse={2}", path,
                    destination, recurse);
                var sourceItem = GetItemForPath(path);

                if (sourceItem == null)
                {
                    SignalPathDoesNotExistError(path);
                    return;
                }

                var destinationItem = GetItemForPath(destination);
                var leafName = sourceItem.Name;

                if (destinationItem == null)
                {
                    leafName = GetLeafFromPath(destination);
                    destination = GetParentFromPath(destination);
                    destinationItem = GetItemForPath(destination);
                    if (destinationItem == null)
                    {
                        SignalPathDoesNotExistError(destination);
                        return;
                    }
                }

                if (ShouldProcess(sourceItem.Paths.Path, "Copy to '" + destinationItem.Paths.Path + "/" + leafName + "'"))
                {
                    var itemCopy = sourceItem.Database.Name == destinationItem.Database.Name
                        ? sourceItem.CopyTo(destinationItem, leafName, new ID(Guid.NewGuid()), recurse)
                        : TransferItem(sourceItem, destinationItem, leafName, recurse);
                    WriteItem(itemCopy);
                }
            }
            catch (Exception ex)
            {
                LogError(ex,
                    "Error while executing CopyItem(string '{0}', string '{1}', bool {2}", path,
                    destination, recurse);
                throw;
            }
        }

        private Item TransferItem(Item sourceItem, Item destinationItem, string leafName, bool recurse)
        {
            using (new ProxyDisabler())
            {
                if (destinationItem.Database.GetTemplate(sourceItem.TemplateID) == null)
                {
                    WriteError(new ErrorRecord(new TemplateNotFoundException(string.Format(
                        "The data contains a reference to a template \"{0}\" that does not exist in the destination database.\nYou must transfer the template first.",
                        sourceItem.Template.FullName)), "sitecore_tranfer_error", ErrorCategory.InvalidData, sourceItem));
                    return null;
                }

                var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
                var transferOptions = TransferOptions.ChangeID;
                if (dic != null && dic[TransferOptionsParam].IsSet)
                {
                    transferOptions = (TransferOptions) dic[TransferOptionsParam].Value;
                }

                var outerXml = ApplicationSettings.SitecoreVersionCurrent >= ApplicationSettings.SitecoreVersion72
                    ? GetSerializedItem72(sourceItem, recurse, transferOptions)
                    : GetSerializedItemOld(sourceItem, recurse, transferOptions);
                var transferedItem = destinationItem.PasteItem(outerXml,
                    transferOptions.HasFlag(TransferOptions.ChangeID),
                    Force ? PasteMode.Overwrite : PasteMode.Undefined);
                Event.RaiseEvent("item:transferred", sourceItem, destinationItem);
                Log.Audit(this, "Transfer from database: {0}, to:{1}", AuditFormatter.FormatItem(sourceItem),
                    AuditFormatter.FormatItem(destinationItem));
                if (transferedItem.Name != leafName)
                {
                    transferedItem.Edit(args => transferedItem.Name = leafName);
                }

                return transferedItem;
            }
        }

        private string GetSerializedItem72(Item sourceItem, bool recurse, TransferOptions transferOptions)
        {
            var options = ItemSerializerOptions.GetDefaultOptions();
            options.AllowDefaultValues = transferOptions.HasFlag(TransferOptions.AllowDefaultValues);
            options.AllowStandardValues = transferOptions.HasFlag(TransferOptions.AllowStandardValues);
            options.ProcessChildren = recurse;
            return sourceItem.GetOuterXml(options);
        }

        private string GetSerializedItemOld(Item sourceItem, bool recurse, TransferOptions transferOptions)
        {
            return sourceItem.GetOuterXml(recurse);
        }

        protected override void MoveItem(string path, string destination)
        {
            try
            {
                LogInfo("Executing MoveItem(string path='{0}', string destination='{1}')",
                    path, destination);
                var sourceItem = GetItemForPath(path);

                if (sourceItem == null)
                {
                    SignalPathDoesNotExistError(path);
                    return;
                }

                if (destination.IndexOf(':') < 0 && path.IndexOf(':') > 0)
                {
                    destination = path.Substring(0, path.IndexOf(':') + 1) + destination;
                }
                var destinationItem = GetItemForPath(destination);
                var leafName = sourceItem.Name;

                if (destinationItem == null)
                {
                    leafName = GetLeafFromPath(destination);
                    destination = GetParentFromPath(destination);
                    destinationItem = GetItemForPath(destination);

                    if (destinationItem == null)
                    {
                        SignalPathDoesNotExistError(destination);
                    }
                }

                if (ShouldProcess(sourceItem.Paths.Path, "Move to '" + destinationItem.Paths.Path + "/" + leafName))
                {
                    sourceItem.MoveTo(destinationItem);

                    if (sourceItem.Name != leafName)
                    {
                        sourceItem.Edit(
                            args => { sourceItem.Name = leafName; });
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex,
                    "Error while executing MoveItem(string path='{0}', string destination='{1}')",
                    path, destination);
                throw;
            }
        }

        protected override void RenameItem(string path, string newName)
        {
            try
            {
                LogInfo("Executing RenameItem(string path='{0}', string newName='{1}')",
                    path, newName);
                var item = GetItemForPath(path);
                if (item != null)
                {
                    CheckOperationAllowed("rename", item.Access.CanRename(), item.Uri.ToString());
                    if (ShouldProcess(item.Paths.Path, "Rename to '" + newName + "'"))
                    {
                        item.Edit(
                            args => { item.Name = newName; });
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex,
                    "Error while executing RenameItem(string path='{0}', string newName='{1}')",
                    path, newName);
                throw;
            }
        }

        protected override void NewItem(string path, string itemTypeName, object newItemValue)
        {
            try
            {
                LogInfo("Executing NewItem(string path='{0}', string itemTypeName='{1}', string newItemValue='{2}')",
                    path, itemTypeName, newItemValue);

                itemTypeName = itemTypeName.Replace('\\', '/').Trim('/');
                // for when the template name is starting with /sitecore/
                if (itemTypeName.StartsWith("sitecore/", StringComparison.OrdinalIgnoreCase))
                {
                    itemTypeName = itemTypeName.Substring(9);
                }
                //for when the /templates at the start was missing
                if (!itemTypeName.StartsWith("templates/", StringComparison.OrdinalIgnoreCase))
                {
                    itemTypeName = "templates/" + itemTypeName;
                }

                var srcItem = GetItemForPath("/" + itemTypeName);

                if (srcItem == null)
                {
                    throw new ObjectNotFoundException(
                        string.Format("Template '{0}' does not exist or wrong path provided.",
                            itemTypeName));
                }
                var parentItem = GetItemForPath(GetParentFromPath(path));

                var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
                if (dic != null && dic[ParentParam].IsSet)
                {
                    parentItem = dic[ParentParam].Value as Item;
                }

                if (ShouldProcess(GetParentFromPath(path),
                    "Create item '" + GetLeafFromPath(path) + "' of type '" + itemTypeName + "'"))
                {
                    Item createdItem = null;
                    if (srcItem.TemplateName == "Template")
                    {
                        createdItem = parentItem.Add(GetLeafFromPath(path), (TemplateItem) srcItem);
                    }
                    else if (srcItem.TemplateName == "Branch")
                    {
                        createdItem = parentItem.Add(GetLeafFromPath(path), (BranchItem) srcItem);
                    }

                    if (dic != null && dic[LanguageParam].IsSet)
                    {
                        var forcedLanguage = dic[LanguageParam].Value.ToString();
                        var language = LanguageManager.GetLanguage(forcedLanguage);
                        // Based on: http://sdn.sitecore.net/Forum/ShowPost.aspx?postid=16551
                        // switching language with LanguageSwitcher didn't work
                        // Thanks Kern!
                        createdItem.Versions.RemoveAll(true);
                        createdItem = createdItem.Database.GetItem(createdItem.ID, language);
                        createdItem = createdItem.Versions.AddVersion();
                    }
                    // start default workflow on the created item if necessary
                    if (dic != null && dic[StartWorkflowParam].IsSet && Context.Workflow.HasDefaultWorkflow(createdItem))
                    {
                        var defaultWorkflow =
                            createdItem.Database.WorkflowProvider.GetWorkflow(createdItem[FieldIDs.DefaultWorkflow]);
                        if (null != defaultWorkflow)
                        {
                            defaultWorkflow.Start(createdItem);
                        }
                    }

                    WriteItem(createdItem);
                }
            }
            catch (Exception ex)
            {
                LogError(ex,
                    "Error while executing NewItem(string path='{0}', string itemTypeName='{1}', string newItemValue='{2}')",
                    path, itemTypeName, newItemValue);
                throw;
            }
        }

        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            return drive;
        }
    }
}