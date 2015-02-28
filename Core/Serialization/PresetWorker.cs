using System.Linq;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Presets;

namespace Cognifide.PowerShell.Core.Serialization
{
    public class PresetWorker
    {
        private int processed;
        private readonly LoadOptions defaultOptions = new LoadOptions {DisableEvents = false};
        private readonly IncludeEntry entry;

        public PresetWorker(IncludeEntry entry)
        {
            this.entry = entry;
        }

        private static int MessageCount
        {
            get
            {
                if (Context.Job == null)
                {
                    return 0;
                }
                return Context.Job.Status.Messages.Cast<string>().Count(m => !m.StartsWith("#"));
            }
        }

        public int Serialize()
        {
            processed = 0;
            if (entry is SingleEntry)
            {
                (entry as SingleEntry).Process(SerializeItem);
            }
            else
            {
                entry.Process(SerializeItem);
            }
            return processed;
        }

        private void SerializeItem(Item item)
        {
            Manager.DumpItem(item);
            processed++;
        }

        public int Deserialize()
        {
            return Deserialize(defaultOptions);
        }

        public int Deserialize(LoadOptions options)
        {
            processed = 0;
            var reference = new ItemReference(entry.Database, entry.Path);
            if (entry is SingleEntry)
            {
                Manager.LoadItem(PathUtils.GetFilePath(reference.ToString()), options);
                processed++;
            }
            else
            {
                var messagesInit = MessageCount;
                Manager.LoadItem(PathUtils.GetFilePath(reference.ToString()), options);
                Manager.LoadTree(PathUtils.GetDirectoryPath(reference.ToString()), options);
                processed += (MessageCount - messagesInit);
            }
            return processed;
        }
    }
}