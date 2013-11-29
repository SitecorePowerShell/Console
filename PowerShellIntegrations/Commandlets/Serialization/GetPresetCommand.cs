using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Xml;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization.Presets;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Serialization
{
    [Cmdlet("Get", "Preset")]
    [OutputType(new[] { typeof(IncludeEntry) })]
    public class GetPresetCommand : BaseCommand
    {
        [Parameter(Position = 0, Mandatory = false)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            WriteObject(GetPresetItems(Name), true);
        }

        private static IEnumerable<IncludeEntry> GetPresetItems(string name)
        {
            IEnumerable<XmlNode> presets = Factory.GetConfigNode("serialization").Cast<XmlNode>();
            if (name != null)
            {
                presets = presets.Where(p => p.Name == name);
            }

            return presets.SelectMany(PresetFactory.Create);
        }
    }
}
