using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Xml;
using Sitecore.Configuration;
using Sitecore.Data.Serialization.Presets;

namespace Cognifide.PowerShell.Commandlets.Serialization
{
    [Cmdlet(VerbsCommon.Get, "Preset")]
    [OutputType(typeof (IncludeEntry))]
    public class GetPresetCommand : BaseCommand
    {
        [Parameter(Position = 0, Mandatory = false)]
        public string[] Name { get; set; }

        protected override void ProcessRecord()
        {
            //WriteObject(GetPresetItems(Name), true);

            var presetsNode = Factory.GetConfigNode("serialization");
            if (presetsNode == null)
            {
                return;
            }

            var allPresets = presetsNode.Cast<XmlNode>().ToList();

            if (Name != null && Name.Length > 0)
            {
                var matchingPresets = WildcardFilterMany(Name, allPresets, p => p.Name).ToList();
                WriteObject(matchingPresets.SelectMany(CreatePreset), true);
            }
            else
            {
                WriteObject(allPresets.SelectMany(CreatePreset), true);
            }
        }

        private static IEnumerable<PSObject> CreatePreset(XmlNode presetNode)
        {
            var presetEntries = PresetFactory.Create(presetNode);
            return presetEntries.Select(preset =>
            {
                var psPreset = PSObject.AsPSObject(preset);
                psPreset.Properties.Add(new PSNoteProperty("PresetName", presetNode.Name));
                return psPreset;
            });
        }
    }
}