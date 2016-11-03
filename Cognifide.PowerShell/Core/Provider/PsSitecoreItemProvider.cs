using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Web;
using Cognifide.PowerShell.Commandlets;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Proxies;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Exceptions;
using Sitecore.Globalization;
using Sitecore.StringExtensions;
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
                Item item;
                if (!TryGetDynamicParam(ItemParam, out item))
                {
                    item = GetItemForPath(path);
                }
                
                if (item == null) return;

                CheckOperationAllowed("remove", item.Access.CanDelete(), item.Uri.ToString());
                if (!ShouldProcess(item.Paths.Path)) return;

                if (IsDynamicParamSet(PermanentlyParam))
                {
                    item.Delete();
                }
                else
                {
                    item.Recycle();
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
                return item != null && item.HasChildren;
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
                string[] language;
                int version;
                GetVersionAndLanguageParams(out version, out language);

                Item item;
                if (!TryGetDynamicParam(ItemParam, out item))
                {
                    string id;
                    path = path.Replace("\\", "/");
                    if (path.Contains("../"))
                    {
                        path = path.Substring(path.LastIndexOf("../", StringComparison.Ordinal) + 2);
                    }

                    item = TryGetDynamicParam(IdParam, out id) ? GetItemById(path, id) : GetItemForPath(path);
                }

                if (IsDynamicParamSet(WithParentParam))
                {
                    GetItemInternal(path, true).ForEach(WriteItem);
                }

                if (item == null)
                {
                    WriteInvalidPathError(VirtualPathUtility.AppendTrailingSlash(item.GetProviderPath()));
                }
                else
                {
                    GetChildItemsHelper(item, recurse, wildcard, language, version);
                }
            }
            catch (Exception ex)
            {
                LogError(ex,
                    "Error while executing GetChildItems(string path='{0}', string recurse='{1}')",
                    path, recurse);
                throw;
            }
        }

        protected void GetChildItemsHelper(Item item, bool recurse, WildcardPattern wildcard, string[] language,
            int version)
        {
            var children = item.GetChildren();
            foreach (Item childItem in children)
            {
                var child = childItem;
                if (wildcard.IsMatch(child.Name))
                {
                    GetMatchingItem(language, version, child).ForEach(WriteItem);
                }

                if (recurse)
                {
                    GetChildItemsHelper(child, true, wildcard, language, version);
                }
            }
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
                    WriteInvalidPathError(path);
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

        protected override void GetItem(string path)
        {
            GetItemInternal(path, true).ForEach(WriteItem);
        }

        internal IEnumerable<Item> GetItemInternal(string path, bool errorIfNotFound)
        {
            LogInfo("Executing GetItem(string path='{0}')", path);

            string[] language;
            int version;

            GetVersionAndLanguageParams(out version, out language);

            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;

            // by Uri
            if (dic != null && dic.ContainsKey(UriParam) && dic[UriParam].IsSet)
            {
                var uri = dic[UriParam].Value.ToString();
                var itemUri = ItemUri.Parse(uri);
                var uriItem = Factory.GetDatabase(itemUri.DatabaseName)
                    .GetItem(itemUri.ItemID, itemUri.Language, itemUri.Version);
                yield return uriItem;
                yield break;
            }

            // by Query
            if (dic != null && dic.ContainsKey(QueryParam) && dic[QueryParam].IsSet)
            {
                var query = dic[QueryParam].Value.ToString();
                var items = Factory.GetDatabase(PSDriveInfo.Name).SelectItems(query);
                foreach (var resultItem in items.SelectMany(currentItem => GetMatchingItem(language, version, currentItem)))
                {
                    yield return resultItem;
                }
                yield break;
            }

            // by Id
            if (dic != null && dic.ContainsKey(IdParam) && dic[IdParam].IsSet)
            {
                var idParam = dic[IdParam].Value.ToString();
                Database database;
                if (dic.ContainsKey(DatabaseParam) && dic[DatabaseParam].IsSet)
                {
                    database = Factory.GetDatabase((string) dic[DatabaseParam].Value);
                }
                else
                {
                    database = Factory.GetDatabase(PSDriveInfo.Name);
                }
                foreach (var id in idParam.Split('|'))
                {

                    var itemById = database.GetItem(new ID(id));
                    foreach (var resultItem in GetMatchingItem(language, version, itemById))
                    {
                        yield return resultItem;
                    }
                }
                yield break;
            }

            var item = GetItemForPath(path);
            if (item != null)
            {
                foreach (var resultItem in GetMatchingItem(language, version, item))
                {
                    yield return resultItem;
                }
            }
            else if(errorIfNotFound)
            {                
                WriteInvalidPathError(path);
            }
        }

        /// <summary>
        ///     Throws an argument exception stating that the specified path does
        ///     not exist.
        /// </summary>
        /// <param name="path">path which is invalid</param>
        private void WriteInvalidPathError(string path)
        {
            var exception = new IOException($"Cannot find path '{path}' because it does not exist.");
            WriteError(new ErrorRecord(exception, ErrorIds.ItemNotFound.ToString(), ErrorCategory.ObjectNotFound, path));
        }

        private IEnumerable<Item> GetMatchingItem(string[] language, int version, Item item)
        {
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            if (dic != null && dic.ContainsKey(AmbiguousPathsParam) && dic[AmbiguousPathsParam].IsSet)
            {
                var ambiguousItems =
                    item.Parent.GetChildren()
                        .Where(child => string.Equals(child.Name, item.Name, StringComparison.CurrentCultureIgnoreCase));
                foreach (var resultItem in ambiguousItems.SelectMany(ambiguousItem => GetMatchingItemEx(language, version, ambiguousItem)))
                {
                    yield return resultItem;
                }
            }
            else
            {
                foreach (var resultItem in GetMatchingItemEx(language, version, item))
                {
                    yield return resultItem;
                }
            }
        }

        private IEnumerable<Item> GetMatchingItemEx(string[] languages, int version, Item item)
        {
            // if language is forced get the item in proper language
            if (languages.Length > 0 || version != Version.Latest.Number)
            {
                var allVersions = item.Versions.GetVersions(languages.Length > 0);

                if (languages.Length > 0)
                {
                    foreach (var language in languages)
                    {
                        var pattern = WildcardUtils.GetWildcardPattern(language);
                        foreach (var matchingItem in allVersions.Where(
                        curItem => (language == null || pattern.IsMatch(curItem.Language.Name)) &&
                                   (version == Int32.MaxValue ||
                                    (version == Version.Latest.Number && curItem.Versions.IsLatestVersion()) ||
                                    (version == curItem.Version.Number)
                                   )))
                        {
                            yield return matchingItem;
                        }
                    }
                }
                else
                {
                    foreach (var matchingItem in allVersions.Where(
                    curItem => version == Int32.MaxValue ||
                               (version == Version.Latest.Number && curItem.Versions.IsLatestVersion()) ||
                               version == curItem.Version.Number))
                    {
                        yield return matchingItem;
                    }
                }
            }
            else
            {
                yield return item;
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

                Item sourceItem;
                if (!TryGetDynamicParam(ItemParam, out sourceItem))
                {
                    sourceItem = GetItemForPath(path);
                }

                if (sourceItem == null)
                {
                    WriteInvalidPathError(path);
                    return;
                }

                Item destinationItem;
                if (!TryGetDynamicParam(DestinationItemParam, out destinationItem))
                {
                    destinationItem = GetItemForPath(destination);
                }

                var leafName = sourceItem.Name;

                if (destinationItem == null)
                {
                    leafName = PathUtilities.GetLeafFromPath(destination);
                    destination = PathUtilities.GetParentFromPath(destination);
                    destinationItem = GetItemForPath(destination);
                    if (destinationItem == null)
                    {
                        WriteInvalidPathError(destination);
                        return;
                    }
                }

                if (!ShouldProcess(sourceItem.Paths.Path, "Copy to '" + destinationItem.Paths.Path + "/" + leafName + "'")) return;

                var itemCopy = sourceItem.Database.Name == destinationItem.Database.Name
                    ? sourceItem.CopyTo(destinationItem, leafName, new ID(Guid.NewGuid()), recurse)
                    : TransferItem(sourceItem, destinationItem, leafName, recurse);
                WriteItem(itemCopy);
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
                    WriteError(new ErrorRecord(new TemplateNotFoundException(
                        $"The data contains a reference to a template \"{sourceItem.Template.FullName}\" that does not exist in the destination database.\nYou must transfer the template first."), ErrorIds.TemplateNotFound.ToString(), ErrorCategory.InvalidData, sourceItem));
                    return null;
                }

                var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
                var transferOptions = TransferOptions.ChangeId;
                if (dic != null && dic[TransferOptionsParam].IsSet)
                {
                    transferOptions = (TransferOptions) dic[TransferOptionsParam].Value;
                }

                var outerXml = string.Empty;
                SitecoreVersion.V72.OrNewer(() =>
                {
                    var options = ItemSerializerOptions.GetDefaultOptions();
                    options.AllowDefaultValues = transferOptions.HasFlag(TransferOptions.AllowDefaultValues);
                    options.AllowStandardValues = transferOptions.HasFlag(TransferOptions.AllowStandardValues);
                    options.ProcessChildren = recurse;
                    outerXml = sourceItem.GetOuterXml(options);
                }).Else(() =>
                {
                    outerXml = sourceItem.GetOuterXml(recurse);
                });

                var transferedItem = destinationItem.PasteItem(outerXml,
                    transferOptions.HasFlag(TransferOptions.ChangeId),
                    Force ? PasteMode.Overwrite : PasteMode.Undefined);
                Event.RaiseEvent("item:transferred", sourceItem, destinationItem);
                PowerShellLog.Audit("Transfer from database: {0}, to:{1}", AuditFormatter.FormatItem(sourceItem),
                    AuditFormatter.FormatItem(destinationItem));
                if (transferedItem.Name != leafName)
                {
                    transferedItem.Edit(args => transferedItem.Name = leafName);
                }

                return transferedItem;
            }
        }

        protected override void MoveItem(string path, string destination)
        {
            try
            {
                LogInfo("Executing MoveItem(string path='{0}', string destination='{1}')",
                    path, destination);
                
                Item sourceItem;
                if (!TryGetDynamicParam(ItemParam, out sourceItem))
                {
                    sourceItem = GetItemForPath(path);
                }

                if (sourceItem == null)
                {
                    WriteInvalidPathError(path);
                    return;
                }

                Item destinationItem;
                if (!TryGetDynamicParam(DestinationItemParam, out destinationItem))
                {
                    if (destination.IndexOf(':') < 0 && path.IndexOf(':') > 0)
                    {
                        destination = path.Substring(0, path.IndexOf(':') + 1) + destination;
                    }
                    destinationItem = GetItemForPath(destination);
                }

                var leafName = sourceItem.Name;

                if (destinationItem == null)
                {
                    leafName = PathUtilities.GetLeafFromPath(destination);
                    destination = PathUtilities.GetParentFromPath(destination);
                    destinationItem = GetItemForPath(destination);

                    if (destinationItem == null)
                    {
                        WriteInvalidPathError(destination);
                        return;
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
                if (itemTypeName.IsNullOrEmpty())
                {
                    WriteError(
                        new ErrorRecord(new InvalidOperationException("Template not provided, please specify -ItemType"),
                            ErrorIds.TemplateNotFound.ToString(), ErrorCategory.InvalidType, path));
                    return;
                }

                var templateItem = TemplateUtils.GetFromPath(itemTypeName, PSDriveInfo.Name);

                var parentItem = GetItemForPath(PathUtilities.GetParentFromPath(path));

                var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
                if (dic != null && dic[ParentParam].IsSet)
                {
                    parentItem = dic[ParentParam].Value as Item;
                }

                if (!ShouldProcess(PathUtilities.GetParentFromPath(path), "Create item '" + PathUtilities.GetLeafFromPath(path) + "' of type '" + itemTypeName + "'")) return;

                var name = PathUtilities.GetLeafFromPath(path);
                Item createdItem = null;

                switch (TemplateUtils.GetType(templateItem))
                {
                    case TemplateItemType.Template:
                        createdItem = parentItem.Add(name, (TemplateItem)templateItem);
                        break;
                    case TemplateItemType.Branch:
                        createdItem = parentItem.Add(name, (BranchItem)templateItem);
                        break;
                    default:
                        WriteError(
                            new ErrorRecord(new InvalidOperationException("Cannot create item as type provided is neither Template nor Branch"),
                                ErrorIds.InvalidItemType.ToString(), ErrorCategory.InvalidType, path));
                        return;
                }

                if (dic != null && dic[LanguageParam].IsSet)
                {
                    var forcedLanguage = dic[LanguageParam].Value.ToString();
                    var language = LanguageManager.GetLanguage(forcedLanguage);
                    createdItem = ForceItemLanguage(createdItem, language);
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
            catch (Exception ex)
            {
                LogError(ex,
                    "Error while executing NewItem(string path='{0}', string itemTypeName='{1}', string newItemValue='{2}')",
                    path, itemTypeName, newItemValue);
                throw;
            }
        }

        private static Item ForceItemLanguage(Item createdItem, Language language)
        {
            // Based on: http://sdn.sitecore.net/Forum/ShowPost.aspx?postid=16551
            // switching language with LanguageSwitcher didn't work
            // Thanks Kern!
            createdItem.Versions.RemoveAll(true);
            createdItem = createdItem.Database.GetItem(createdItem.ID, language);
            createdItem = createdItem.Versions.AddVersion();
            foreach (Item child in createdItem.Children)
            {
                ForceItemLanguage(child, language);
            }
            return createdItem;
        }

        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            return drive;
        }

    }
}