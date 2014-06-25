using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "FieldEditor")]
    [OutputType(new[] {typeof (string)}, ParameterSetName = new[]
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
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Item from Pipeline, Preserve Sections")]
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Item from Pipeline, Named Section")]
        public override Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from Path, Named Section")]
        [Alias("FullName", "FileName")]
        public override string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from ID, Named Section")]
        public override string Id { get; set; }

        [Parameter(ParameterSetName = "Item from ID, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from ID, Named Section")]
        public override Database Database { get; set; }

        [Alias("Languages")]
        [Parameter(ParameterSetName = "Item from Path, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from ID, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from Path, Named Section")]
        [Parameter(ParameterSetName = "Item from ID, Named Section")]
        public virtual string[] Language { get; set; }


        [Parameter(ParameterSetName = "Item from Pipeline, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from Path, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from ID, Preserve Sections")]
        public SwitchParameter PreserveSections { get; set; }

        [Parameter(ParameterSetName = "Item from Pipeline, Named Section")]
        [Parameter(ParameterSetName = "Item from Path, Named Section")]
        [Parameter(ParameterSetName = "Item from ID, Named Section")]
        public string SectionTitle { get; set; }

        [Parameter(ParameterSetName = "Item from Pipeline, Named Section")]
        [Parameter(ParameterSetName = "Item from Path, Named Section")]
        [Parameter(ParameterSetName = "Item from ID, Named Section")]
        public string SectionIcon { get; set; }

        [Parameter(ParameterSetName = "Item from Pipeline, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from Path, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from ID, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from Pipeline, Named Section")]
        [Parameter(ParameterSetName = "Item from Path, Named Section")]
        [Parameter(ParameterSetName = "Item from ID, Named Section")]
        public string[] FieldName { get; set; }

        [Parameter(ParameterSetName = "Item from Pipeline, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from Path, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from ID, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from Pipeline, Named Section")]
        [Parameter(ParameterSetName = "Item from Path, Named Section")]
        [Parameter(ParameterSetName = "Item from ID, Named Section")]
        public string Title { get; set; }

        [Parameter(ParameterSetName = "Item from Pipeline, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from Path, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from ID, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from Pipeline, Named Section")]
        [Parameter(ParameterSetName = "Item from Path, Named Section")]
        [Parameter(ParameterSetName = "Item from ID, Named Section")]
        public int Width { get; set; }

        [Parameter(ParameterSetName = "Item from Pipeline, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from Path, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from ID, Preserve Sections")]
        [Parameter(ParameterSetName = "Item from Pipeline, Named Section")]
        [Parameter(ParameterSetName = "Item from Path, Named Section")]
        [Parameter(ParameterSetName = "Item from ID, Named Section")]
        public int Height { get; set; }

        public ShowFieldEditorCommand()
        {
            Width = 800;
            Height = 600;
        }

        protected override void BeginProcessing()
        {
            LogErrors(() => BaseShellCommand.EnsureSiteContext());
        }

        protected override void ProcessItem(Item item)
        {
            LogErrors(() =>
            {
                if (Context.Job != null)
                {
                    var fields = "*";
                    if (FieldName != null && FieldName.Length > 0)
                        fields = FieldName.Aggregate((current, next) => current + "|" + next);
                    var icon = string.IsNullOrEmpty(SectionIcon) ? Item.Appearance.Icon : SectionIcon;
                    var sectionTitle = string.IsNullOrEmpty(SectionTitle) ? Item.Name : SectionTitle;
                    var message = new ShellCommandInItemContextMessage(Item,
                        "powershell:fieldeditor(title=" + (string.IsNullOrEmpty(Title) ? Item.Name : Title) +
                        ",preservesections=" + (PreserveSections ? "1" : "0") +
                        ",fields=" + fields +
                        ",icon=" + icon +
                        ",section=" + sectionTitle +
                        ",width=" + Width +
                        ",height=" + Height +
                        ")");
                    BaseShellCommand.PutMessage(message);
                    var result = BaseShellCommand.GetResult(message).ToString();
                    WriteObject(result);
                }
            });
        }
    }
}