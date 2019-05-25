using System.Collections.Generic;
using Sitecore.Data.Items;
using Sitecore.Tasks;
using Spe.Core.Extensions;
using Spe.Core.Host;
using Spe.Core.Settings;
using Spe.Core.Utility;

namespace Spe.Integrations.Tasks
{
    public class ExecuteScriptTask
    {
        public void Update(Item[] items, CommandItem command, ScheduleItem schedule)
        {
            using (var session = ScriptSessionManager.NewSession(ApplicationNames.Default, true))
            {
                foreach (var item in items)
                {
                    ProcessTaskItem(item, session);
                }
            }
        }

        private static void ProcessTaskItem(Item item, ScriptSession session)
        {
            var queue = new Queue<Item>();
            queue.Enqueue(item);

            Item currentItem;
            while (queue.Count > 0 && (currentItem = queue.Dequeue()) != null)
            {
                if (currentItem.IsPowerShellScript())
                {
                    if (string.IsNullOrWhiteSpace(currentItem[Templates.Script.Fields.ScriptBody])) continue;
                    if (!RulesUtils.EvaluateRules(currentItem[Templates.Script.Fields.EnableRule], currentItem)) continue;

                    session.SetItemLocationContext(currentItem);
                    session.ExecuteScriptPart(currentItem, true);
                }
                else if (currentItem.IsPowerShellLibrary() && currentItem.HasChildren)
                {
                    if (!RulesUtils.EvaluateRules(currentItem[Templates.Script.Fields.EnableRule], currentItem)) continue;

                    var children = currentItem.Children.ToArray();
                    foreach (var child in children)
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }
    }
}