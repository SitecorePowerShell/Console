using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Sites;
using Sitecore.Workflows;
using Sitecore.Workflows.Simple;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Get, "Rendering")]
    [OutputType(new[] {typeof (RenderingReference)}, ParameterSetName = new[] { "Item from Pipeline", "Item from Path", "Item from ID" })]
    public class GetRenderingCommand : BaseItemCommand
    {
        [Parameter]
        public DeviceItem Device { get; set; }

        [Parameter]
        public string Placeholder { get; set; }

        protected override void ProcessItem(Item item)
        {
            LayoutDefinition layout =
              LayoutDefinition.Parse(Item[Sitecore.FieldIDs.LayoutField]);
            //todo: actually implement this :)
        }
    }
}