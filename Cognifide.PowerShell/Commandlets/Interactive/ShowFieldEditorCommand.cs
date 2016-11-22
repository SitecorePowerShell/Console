using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Cognifide.PowerShell.Core.Validation;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "FieldEditor")]
    [OutputType(typeof (string), ParameterSetName = new[]
    {
        "Item from Path, Preserve Sections",
        "Item from ID, Preserve Sections",
        "Item from Path, Named Section",
        "Item from ID, Named Section",
        "Item from Pipeline, Preserve Sections",
        "Item from Pipeline, Named Section"
    })]
    public class ShowFieldEditorCommand : BaseItemCommand
    {
        public ShowFieldEditorCommand()
        {
            Width = 800;
            Height = 600;
        }

        [Parameter]
        [Alias("FieldName")]
        public string[] Name { get; set; }

        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public int Width { get; set; }

        [Parameter]
        public int Height { get; set; }

        [Parameter]
        public SwitchParameter IncludeStandardFields { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Item from Pipeline, Preserve Sections", Mandatory = true)]
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Item from Pipeline, Named Section", Mandatory = true)]
        public override Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path, Preserve Sections", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from Path, Named Section", Mandatory = true)]
        [Alias("FullName", "FileName")]
        public override string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID, Preserve Sections", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from ID, Named Section", Mandatory = true)]
        public override string Id { get; set; }

        [AutocompleteSet("Databases")]
        [Parameter(ParameterSetName = "Item from ID, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from ID, Named Section")]
        public override string Database { get; set; }

        [Alias("Languages")]
        [Parameter(ParameterSetName = "Item from Path, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from ID, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from Path, Named Section")]
        [Parameter(ParameterSetName = "Item from ID, Named Section")]
        public override string[] Language { get; set; }

        [Parameter(ParameterSetName = "Item from Pipeline, Preserve Sections", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from Path, Preserve Sections", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from ID, Preserve Sections", Mandatory = true)]
        public SwitchParameter PreserveSections { get; set; }

        [Parameter(ParameterSetName = "Item from Pipeline, Named Section")]
        [Parameter(ParameterSetName = "Item from Path, Named Section")]
        [Parameter(ParameterSetName = "Item from ID, Named Section")]
        public string SectionTitle { get; set; }

        [Parameter(ParameterSetName = "Item from Pipeline, Named Section")]
        [Parameter(ParameterSetName = "Item from Path, Named Section")]
        [Parameter(ParameterSetName = "Item from ID, Named Section")]
        public string SectionIcon { get; set; }

        protected override void ProcessItem(Item item)
        {
            LogErrors(() =>
            {
                if (!CheckSessionCanDoInteractiveAction()) return;

                if (Context.Job != null)
                {
                    var fields = "*";
                    if (Name != null && Name.Length > 0)
                        fields = Name.Aggregate((current, next) => current + "|" + next);
                    var icon = string.IsNullOrEmpty(SectionIcon) ? item.Appearance.Icon : SectionIcon;
                    var sectionTitle = string.IsNullOrEmpty(SectionTitle) ? item.Name : SectionTitle;
                    var message = new ShellCommandInItemContextMessage(item,
                        "powershell:fieldeditor(title=" + (string.IsNullOrEmpty(Title) ? item.Name : Title) +
                        ",preservesections=" + (PreserveSections ? "1" : "0") +
                        ",fields=" + fields +
                        ",icon=" + icon +
                        ",section=" + sectionTitle +
                        ",width=" + Width +
                        ",height=" + Height +
                        ",isf=" + (IncludeStandardFields ? "1" : "0") +
                        ")");

                    PutMessage(message);
                    var result = message.GetResult().ToString();
                    WriteObject(result);
                }
            });
        }
    }
}