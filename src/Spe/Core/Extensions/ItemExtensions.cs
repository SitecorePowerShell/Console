using System;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Resources.Media;

namespace Spe.Core.Extensions
{
    public static class ItemExtensions
    {
        public static bool InheritsFrom(this Item item, ID templateId)
        {
            return item != null && TemplateManager.GetTemplate(item) is Template template &&
                   template.InheritsFrom(templateId);
        }

        public static bool IsPowerShellScript(this Item item)
        {
            return item.InheritsFrom(Templates.Script.Id);
        }

        public static bool IsPowerShellScriptTemplateField(this Item item)
        {
            return item.ID == Templates.Script.Fields.ScriptBody;
        }

        public static bool IsPowerShellModule(this Item item)
        {
            return item.InheritsFrom(Templates.ScriptModule.Id);
        }

        public static bool IsPowerShellModuleFolder(this Item item)
        {
            return item.InheritsFrom(Templates.ScriptModuleFolder.Id);
        }

        public static bool IsPowerShellLibrary(this Item item)
        {
            return item.InheritsFrom(Templates.ScriptLibrary.Id);
        }

        public static void Edit(this Item item, Action<ItemEditArgs> action)
        {
            var args = new ItemEditArgs() { Item = item };
            try
            {
                var wasEditing = item.Editing.IsEditing;
                if (!wasEditing)
                {
                    item.Editing.BeginEdit();
                }
                action(args);
                if (!wasEditing)
                {
                    if (args.Save)
                    {
                        item.Editing.EndEdit(args.UpdateStatistics, args.Silent);
                    }
                    else
                    {
                        item.Editing.CancelEdit();
                    }
                }
            }
            catch
            {
                if (args.SaveOnError)
                {
                    item.Editing.EndEdit(args.UpdateStatistics, args.Silent);
                }
                else
                {
                    item.Editing.CancelEdit();
                }
                throw;
            }
        }

        public class ItemEditArgs
        {
            /// <summary>
            ///     is set to true this instance will update statistics
            ///     default: true
            /// </summary>
            public bool UpdateStatistics { get; set; } = true;

            /// <summary>
            ///     if set to true this instance is silent
            ///     default: false
            /// </summary>
            public bool Silent { get; set; }

            /// <summary>
            ///     if set to true a succesful operation will result in item being saved
            ///     default: true
            /// </summary>
            public bool Save { get; set; } = true;

            /// <summary>
            ///     if set to true the edited item will get saved despite exceptions in clause code
            ///     default: false
            /// </summary>
            public bool SaveOnError { get; set; }

            /// <summary>
            ///     Edited Item
            ///     default: false
            /// </summary>
            public Item Item { get; set; }
        }

        public static bool IsVersioned(this Item item)
        {
            var mediaData = new MediaData(new MediaItem(item));
            var field = item.Fields[mediaData.DataFieldName];
            if (!field.Shared)
            {
                return !field.Unversioned;
            }

            return false;
        }
        
        public static Item FirstExistingVersion(this Item item)
        {
            if (item != null && item.Versions.Count == 0)
            {
                var allVersions = item.Versions.GetVersions(true);
                if (allVersions.Any())
                {
                    return allVersions.FirstOrDefault();
                }
            }

            return item;
        }
    }
}