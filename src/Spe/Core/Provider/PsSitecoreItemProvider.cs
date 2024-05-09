using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Threading.Tasks;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Archiving;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Exceptions;
using Sitecore.Globalization;
using Sitecore.StringExtensions;
using Spe.Commands;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Utility;
using Version = Sitecore.Data.Version;

namespace Spe.Core.Provider
{
    [CmdletProvider("PsSitecoreItemProvider",
        ProviderCapabilities.Filter | ProviderCapabilities.ShouldProcess | ProviderCapabilities.ExpandWildcards)]
    [OutputType(typeof(Item), ProviderCmdlet = "Get-ChildItem")]
    [OutputType(typeof(Item), ProviderCmdlet = "Get-Item")]
    [OutputType(typeof(Item), ProviderCmdlet = "New-Item")]
    [OutputType(typeof(Item), ProviderCmdlet = "Copy-Item")]
    public partial class PsSitecoreItemProvider : NavigationCmdletProvider, IPropertyCmdletProvider
    {

        /// <summary>
        ///     Moves page to recycle bin on remove-item
        /// </summary>
        /// <param name="path"></param>
        /// <param name="recurse"></param>
        protected override void RemoveItem(string path, bool recurse)
        {
            try
            {
                LogInfo("Executing RemoveItem(string path='{0}', string recurse='{1}')", path, recurse);
                if (!TryGetDynamicParam(ItemParam, out Item item))
                {
                    item = GetItemForPath(path);
                }

                if (item == null) return;

                CheckOperationAllowed("remove", item.Access.CanDelete(), item.Uri.ToString());
                if (!ShouldProcess(item.Paths.Path)) return;

                var hasArchive = IsDynamicParamSet(ArchiveParam);
                var hasPermanently = IsDynamicParamSet(PermanentlyParam);

                if (hasArchive && hasPermanently)
                {
                    var error = new ErrorRecord(new ParameterBindingException("Parameter set cannot be resolved using the specified named parameters. Detected Archive and Permanently parameters provided."), ErrorIds.AmbiguousParameterSet.ToString(), ErrorCategory.InvalidOperation, null);
                    WriteError(error);
                    return;
                }

                if (hasArchive)
                {
                    var archive = ArchiveManager.GetArchive("archive", item.Database);
                    WriteVerbose($"Removing item {item.ID} and moving to the archive {archive.Name} in database {item.Database}");
                    archive.ArchiveItem(item);
                }
                else if (hasPermanently)
                {
                    WriteVerbose($"Removing item {item.ID} permanently");
                    item.Delete();
                }
                else
                {
                    WriteVerbose($"Removing item {item.ID} and moving to the recycle bin in database {item.Database}");
                    item.Recycle();
                }
            }
            catch (Exception ex)
            {
                LogError(ex,
                    "Error while executing RemoveItem(string path='{0}', string recurse='{1}')",
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

        protected void GetChildItemsWithDepth(string path, bool recurse, uint depth)
        {
            try
            {
                LogInfo("Executing GetChildItems(string path='{0}', string recurse='{1}')", path, recurse);
                var wildcard = new WildcardPattern(Filter ?? "*", WildcardOptions.IgnoreCase | WildcardOptions.Compiled);

                GetVersionAndLanguageParams(out int version, out string[] language);

                string id = null;
                if (!TryGetDynamicParam(ItemParam, out Item item))
                {
                    path = path.Replace("\\", "/");
                    if (path.Contains("../"))
                    {
                        path = path.Substring(path.LastIndexOf("../", StringComparison.Ordinal) + 2);
                    }

                    item = TryGetDynamicParam(IdParam, out id) ? GetItemById(path, id) : GetItemForPath(path);
                }
                else
                {
                    path = item.GetProviderPath();
                }

                if (IsDynamicParamSet(WithParentParam))
                {
                    GetItemInternal(path, true).ForEach(WriteItem);
                }

                if (item == null)
                {
                    WriteInvalidPathError(path, id);
                }
                else
                {
                    GetChildItemsHelper(item, recurse, wildcard, language, version, 0, depth);
                }
            }
            catch (PipelineStoppedException)
            {
                // pipeline stopped e.g. if we did:
                // Get-ChildItem master:\ | Select-Object -First 1 
                // we can relax now - no more items needed
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex,
                    "Error while executing GetChildItems(string path='{0}', string recurse='{1}')",
                    path, recurse);
                throw;
            }
        }

        protected override void GetChildItems(string path, bool recurse)
        {
            if (!TryGetDynamicParam(DepthParam, out uint depth))
            {
                depth = uint.MaxValue;
            }
            GetChildItemsWithDepth(path, recurse, depth);
        }

        protected void GetChildItemsHelper(Item item, bool recurse, WildcardPattern wildcard, string[] language,
            int version, uint currentDepth, uint depth)
        {
            var children = item.GetChildren();
            foreach (Item childItem in children)
            {
                var child = childItem;
                if (wildcard.IsMatch(child.Name))
                {
                    GetMatchingItem(language, version, child, false).ForEach(WriteItem);
                }

                if (recurse && currentDepth < depth)
                {
                    var nextDepth = currentDepth + 1;
                    GetChildItemsHelper(child, true, wildcard, language, version, nextDepth, depth);
                }
            }
        }

        protected override string GetChildName(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            path = NormalizePath(path);
            path = path.TrimEnd('\\');
            var index = path.LastIndexOf('\\');
            var result = path.Substring(index + 1);
            //var result = base.GetChildName(path);
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
            catch (PipelineStoppedException)
            {
                // pipeline stopped e.g. if we did:
                // Get-ChildItem master:\ | Select-Object -First 1 
                // we can relax now - no more items needed
                throw;
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

            GetVersionAndLanguageParams(out var version, out var language);
            if (language != null && !language.Any()) yield break;
            language = language ?? new string[0];

            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;

            // by Uri
            if (dic != null && dic.ContainsKey(UriParam) && dic[UriParam].IsSet)
            {
                var uri = dic[UriParam].Value.ToString();
                var itemUri = ItemUri.Parse(uri);
                var uriItem = Factory.GetDatabase(itemUri.DatabaseName)
                    .GetItem(itemUri.ItemID, itemUri.Language, itemUri.Version);
                if (uriItem != null)
                {
                    yield return uriItem;
                }
                else if (errorIfNotFound)
                {
                    WriteInvalidPathError(uri);
                }
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
                    database = Factory.GetDatabase((string)dic[DatabaseParam].Value);
                }
                else
                {
                    database = Factory.GetDatabase(PSDriveInfo.Name);
                }
                foreach (var id in idParam.Split('|'))
                {

                    var itemById = database.GetItem(new ID(id));
                    if (itemById != null)
                    {
                        foreach (var resultItem in GetMatchingItem(language, version, itemById))
                        {
                            yield return resultItem;
                        }
                    } 
                    else if(errorIfNotFound)
                    {
                        WriteInvalidPathError(database.Name, id);
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
            else if (errorIfNotFound)
            {
                WriteInvalidPathError(path);
            }
        }

        private void WriteInvalidPathError(string path, string id = null)
        {
            var message = $"Cannot find path '{path}' because it does not exist.";
            if (!id.IsNullOrEmpty())
            {
                message = $"Cannot find path '{path}' and id {id} because it does not exist.";
            }
            var exception = new IOException(message);
            WriteError(new ErrorRecord(exception, ErrorIds.ItemNotFound.ToString(), ErrorCategory.ObjectNotFound, path));
        }

        private IEnumerable<Item> GetMatchingItem(string[] language, int version, Item item, bool checkAmbiguousPath = true)
        {
            if (checkAmbiguousPath && DynamicParameters is RuntimeDefinedParameterDictionary dic &&
                dic.ContainsKey(AmbiguousPathsParam) &&
                dic[AmbiguousPathsParam].IsSet)
            {
                var ambiguousItems =
                    item.Parent.GetChildren()
                        .Where(child => string.Equals(child.Name, item.Name, StringComparison.CurrentCultureIgnoreCase));
                foreach (
                    var resultItem in
                    ambiguousItems.SelectMany(ambiguousItem => GetMatchingItemEx(language, version, ambiguousItem)))
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
            if (languages != null && (languages.Length > 0 || version != Version.Latest.Number))
            {
                var allVersions = item.Versions.GetVersions(languages.Length > 0);
                TryGetDynamicParam(WithMissingLanguagesParam, out SwitchParameter addMissingLanguages);

                if (languages.Length > 0)
                {
                    foreach (var language in languages)
                    {
                        var languageHandled = false;
                        var pattern = WildcardUtils.GetWildcardPattern(language);
                        foreach (var matchingItem in allVersions.Where(
                            curItem => (language == null || pattern.IsMatch(curItem.Language.Name)) &&
                                       (version == int.MaxValue ||
                                        version == Version.Latest.Number && curItem.Versions.IsLatestVersion() ||
                                        version == curItem.Version.Number
                                       )))
                        {
                            languageHandled = true;
                            yield return matchingItem;
                        }
                        if (addMissingLanguages && !languageHandled && !language.IsWildcard())
                        {
                            yield return item.Database.GetItem(item.ID, LanguageManager.GetLanguage(language));
                        }
                    }
                }
                else
                {
                    foreach (var matchingItem in allVersions.Where(
                    curItem => version == int.MaxValue ||
                               version == Version.Latest.Number && curItem.Versions.IsLatestVersion() ||
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
            if (item == null) return;

            var psObject = ItemShellExtensions.GetPsObject(SessionState, item);
            var path = item.Database.Name + ":" + item.Paths.Path.Substring(9).Replace('/', '\\');
            WriteItemObject(psObject, path, item.HasChildren);
        }

        protected override void CopyItem(string path, string destination, bool recurse)
        {
            try
            {
                LogInfo("Executing CopyItem(string path='{0}', string destination='{1}', bool recurse={2}", path,
                    destination, recurse);

                if (!TryGetDynamicParam(ItemParam, out Item sourceItem))
                {
                    sourceItem = GetItemForPath(path);
                }

                if (sourceItem == null)
                {
                    WriteInvalidPathError(path);
                    return;
                }

                if (!TryGetDynamicParam(DestinationItemParam, out Item destinationItem))
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
            catch (PipelineStoppedException)
            {
                // pipeline stopped e.g. by `Select-Object -First 1`
                throw;
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
                transferOptions = (TransferOptions)dic[TransferOptionsParam].Value;
            }

            var options = ItemSerializerOptions.GetDefaultOptions();
            options.AllowDefaultValues = transferOptions.HasFlag(TransferOptions.AllowDefaultValues);
            options.AllowStandardValues = transferOptions.HasFlag(TransferOptions.AllowStandardValues);
            options.ProcessChildren = recurse;
            var outerXml = sourceItem.GetOuterXml(options);

            var transferredItem = destinationItem.PasteItem(outerXml,
                transferOptions.HasFlag(TransferOptions.ChangeId),
                Force ? PasteMode.Overwrite : PasteMode.Undefined);
            if (sourceItem.Paths.IsMediaItem)
            {
                if (sourceItem.TemplateID != TemplateIDs.MediaFolder && sourceItem.IsVersioned())
                {
                    TransferVersionedMediaItemBlob(sourceItem, destinationItem, recurse);
                }
                else
                {
                    TransferMediaItemBlob(sourceItem, destinationItem, recurse);
                }
            }
            Event.RaiseEvent("item:transferred", sourceItem, destinationItem);
            PowerShellLog.Audit("Transfer from database: {0}, to:{1}", AuditFormatter.FormatItem(sourceItem),
                AuditFormatter.FormatItem(destinationItem));
            if (transferredItem.Name != leafName)
            {
                transferredItem.Edit(args => transferredItem.Name = leafName);
            }

            return transferredItem;
        }

        private void TransferVersionedMediaItemBlob(Item source, Item destination, bool processChildren)
        {
            Assert.IsNotNull(source, "source is null");
            Assert.IsNotNull(destination, "destination is null");
            Parallel.ForEach(source.Languages, (language =>
            {
                var itemByLanguage = source.Database.GetItem(source.ID, language);
                if (itemByLanguage == null || itemByLanguage.Versions.Count <= 0) { return; }
                var versions = ItemManager.GetVersions(itemByLanguage);
                if (versions == null) { return; }
                foreach (var version in versions)
                {
                    var itemByLanguageAndVersion = itemByLanguage.Database.GetItem(source.ID, language, version);
                    if (itemByLanguageAndVersion != null)
                    {
                        TransferMediaItemBlob(itemByLanguageAndVersion, destination, processChildren);
                    }
                }
            }));
        }

        private void TransferMediaItemBlob(Item source, Item destination, bool processChildren)
        {
            Assert.IsNotNull(source, "source is null");
            Assert.IsNotNull(destination, "destination is null");
            foreach (Field field in source.Fields)
            {
                if (field.IsBlobField)
                {
                    string str = field.Value;
                    if (str.Length > 38)
                    {
                        str = str.Substring(0, 38);
                    }
                    var guid = MainUtil.GetGuid(str, Guid.Empty);
                    if (!(guid == Guid.Empty))
                    {
                        var blobStream = ItemManager.GetBlobStream(guid, source.Database);
                        if (blobStream != null)
                        {
                            using (blobStream)
                            {
                                ItemManager.SetBlobStream(blobStream, guid, destination.Database);
                            }
                        }
                    }
                }
            }

            if (!processChildren) { return; }

            foreach (Item child in source.Children)
            {
                if (child != null)
                {
                    if (child.TemplateID != TemplateIDs.MediaFolder && child.IsVersioned())
                    {
                        TransferVersionedMediaItemBlob(child, destination, true);
                    }
                    else
                    {
                        TransferMediaItemBlob(child, destination, true);
                    }
                }
            }
        }

        protected override void MoveItem(string path, string destination)
        {
            try
            {
                LogInfo("Executing MoveItem(string path='{0}', string destination='{1}')",
                    path, destination);

                if (!TryGetDynamicParam(ItemParam, out Item sourceItem))
                {
                    sourceItem = GetItemForPath(path);
                }

                if (sourceItem == null)
                {
                    WriteInvalidPathError(path);
                    return;
                }

                if (!TryGetDynamicParam(DestinationItemParam, out Item destinationItem))
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

                if (!ShouldProcess(sourceItem.Paths.Path,
                    "Move to '" + destinationItem.Paths.Path + "/" + leafName)) return;

                sourceItem.MoveTo(destinationItem);

                if (sourceItem.Name != leafName)
                {
                    sourceItem.Edit(
                        args => { sourceItem.Name = leafName; });
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

                if (parentItem == null)
                {
                    WriteError(
                        new ErrorRecord(new InvalidOperationException($"Could not find a part of the path '{path}'"),
                            ErrorIds.NewItemIOError.ToString(), ErrorCategory.WriteError, path));
                    return;
                }

                var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
                if (dic != null && dic[ParentParam].IsSet)
                {
                    parentItem = dic[ParentParam].Value as Item;

                    if (parentItem == null)
                    {
                        WriteError(
                            new ErrorRecord(new InvalidOperationException($"Could not continue because the Parent parameter is null"),
                                ErrorIds.NewItemIOError.ToString(), ErrorCategory.WriteError, path));
                        return;
                    }
                }

                if (!ShouldProcess(PathUtilities.GetParentFromPath(path),
                    "Create item '" + PathUtilities.GetLeafFromPath(path) + "' of type '" + itemTypeName + "'"))
                {
                    return;
                }

                var forcedId = ID.Null;

                if (dic != null && dic[ForceIdParam].IsSet)
                {
                    var forcedIdString = dic[ForceIdParam].Value as String;
                    if (ID.IsID(forcedIdString))
                    {
                        forcedId = ID.Parse(forcedIdString);
                    }
                }

                var name = PathUtilities.GetLeafFromPath(path);
                Item createdItem;

                switch (TemplateUtils.GetType(templateItem))
                {
                    case TemplateItemType.Template:
                    case TemplateItemType.Branch:
                        createdItem = forcedId != ID.Null
                            ? ItemManager.AddFromTemplate(name, templateItem.ID, parentItem, forcedId)
                            : ItemManager.AddFromTemplate(name, templateItem.ID, parentItem);
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
                    defaultWorkflow?.Start(createdItem);
                }

                WriteItem(createdItem);
            }
            catch (PipelineStoppedException)
            {
                // pipeline stopped e.g. by `Select-Object -First 1`
                throw;
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