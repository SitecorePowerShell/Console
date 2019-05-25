using System.Collections.Generic;
using System.Web.UI;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using Spe.Core.Extensions;
using Spe.Core.Modules;

namespace Spe.Client.Controls
{
    public class IsePluginStrip : RibbonStrip
    {
        public override void Render(HtmlTextWriter output, Ribbon ribbon, Item strip, CommandContext context)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(ribbon, "ribbon");
            Assert.ArgumentNotNull(strip, "strip");
            Assert.ArgumentNotNull(context, "context");

            List<Ribbon.Chunk> chunks = new List<Ribbon.Chunk>();
            var contextChunks = new List<Item>();
            foreach (var root in ModuleManager.GetFeatureRoots(IntegrationPoints.IsePluginFeature))
            {
                if (!root.HasChildren)
                {
                    continue;
                }

                Item parent = root.Parent;
                while (parent != null && !parent.IsPowerShellModule())
                {
                    parent = parent.Parent;
                }
                if (parent == null)
                {
                    continue;
                }
                
                Ribbon.Chunk chunk = new Ribbon.Chunk
                {
                    Header = parent.DisplayName,
                    Click = string.Format("ise:chunk(libraryDB={0},libraryItem={1})", root.Database.Name, root.ID)
                };
                chunks.Add(chunk);
                contextChunks.Add(root);

                Ribbon.Button button = new Ribbon.Button
                {
                    ReferenceID = new ID("{4AB06023-6462-4FA4-BC56-27918ACC7D55}")
                };
                chunk.Buttons.Add(button);

            }
            context.CustomData = contextChunks;
            ribbon.RenderStrip(output, chunks);
        }
    }
}