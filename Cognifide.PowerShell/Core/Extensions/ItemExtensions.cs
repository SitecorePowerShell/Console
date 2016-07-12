using System;
using Cognifide.PowerShell.Core.Settings;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Core.Extensions
{
    public static class ItemExtensions
    {
        public static bool IsPowerShellScript(this Item item)
        {
            return item != null &&
                   item.TemplateName.Equals(TemplateNames.ScriptTemplateName, StringComparison.InvariantCulture);
        }

        public static bool IsPowerShellModule(this Item item)
        {
            return item != null &&
                   item.TemplateName.Equals(TemplateNames.ScriptModuleTemplateName, StringComparison.InvariantCulture);
        }

        public static bool IsPowerShellModuleFolder(this Item item)
        {
            return item != null &&
                   item.TemplateName.Equals(TemplateNames.ScriptModuleFolderTemplateName, StringComparison.InvariantCulture);
        }

        public static bool IsPowerShellLibrary(this Item item)
        {
            return item != null &&
                   item.TemplateName.Equals(TemplateNames.ScriptLibraryTemplateName, StringComparison.InvariantCulture);
        }

        public static void Edit(this Item item, Action<ItemEditArgs> action)
        {
            var args = new ItemEditArgs();
            try
            {
                bool wasEditing = item.Editing.IsEditing;
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
            public ItemEditArgs()
            {
                UpdateStatistics = true;
                Save = true;
            }

            /// <summary>
            ///     is set to true this instance will update statistics
            ///     default: true
            /// </summary>
            public bool UpdateStatistics { get; set; }

            /// <summary>
            ///     if set to true this instance is silent
            ///     default: false
            /// </summary>
            public bool Silent { get; set; }

            /// <summary>
            ///     if set to true a succesful operation will result in item being saved
            ///     default: true
            /// </summary>
            public bool Save { get; set; }

            /// <summary>
            ///     if set to true the edited item will get saved despite exceptions in clause code
            ///     default: false
            /// </summary>
            public bool SaveOnError { get; set; }
        }
    }
}