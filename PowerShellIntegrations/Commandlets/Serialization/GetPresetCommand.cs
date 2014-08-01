using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Xml;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization.Presets;
using Sitecore.Shell.Applications.Layouts.PageDesigner.Commands;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Serialization
{
    [Cmdlet(VerbsCommon.Get, "Preset")]
    [OutputType(new[] { typeof(IncludeEntry) })]
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

        private IEnumerable<PSObject> CreatePreset(XmlNode presetNode)
        {
            IList<IncludeEntry> presetEntries = PresetFactory.Create(presetNode);
            return presetEntries.Select(preset =>
            {
                PSObject psPreset = PSObject.AsPSObject(preset);
                psPreset.Properties.Add(new PSNoteProperty("PresetName", presetNode.Name));
                return psPreset;
            });
        }
    }
}
