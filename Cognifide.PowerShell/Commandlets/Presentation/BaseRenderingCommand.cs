using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Cognifide.PowerShell.Core.Validation;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Text;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    public abstract class BaseRenderingCommand : BaseLayoutPerDeviceCommand
    {
        private int index = -1;
        private Dictionary<string, WildcardPattern> paramPatterns;

        [Parameter(Mandatory = true, ParameterSetName = "Rendering by filter, Item from Pipeline",
            ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Mandatory = true, ParameterSetName = "Rendering by instance, Item from Pipeline",
            ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Mandatory = true, ParameterSetName = "Rendering by unique ID, Item from Pipeline",
            ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public override Item Item { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Rendering by filter, Item from Path")]
        [Parameter(Mandatory = true, ParameterSetName = "Rendering by instance, Item from Path")]
        [Parameter(Mandatory = true, ParameterSetName = "Rendering by unique ID, Item from Path")]
        [Alias("FullName", "FileName")]
        public override string Path { get; set; }

        [Parameter(ParameterSetName = "Rendering by filter, Item from ID")]
        [Parameter(ParameterSetName = "Rendering by instance, Item from ID")]
        [Parameter(ParameterSetName = "Rendering by unique ID, Item from ID")]
        public override string Id { get; set; }

        [AutocompleteSet("Databases")]
        [Parameter(ParameterSetName = "Rendering by filter, Item from ID")]
        [Parameter(ParameterSetName = "Rendering by instance, Item from ID")]
        [Parameter(ParameterSetName = "Rendering by unique ID, Item from ID")]
        public override string Database { get; set; }

        [Parameter(ParameterSetName = "Rendering by filter, Item from Pipeline")]
        [Parameter(ParameterSetName = "Rendering by filter, Item from Path")]
        [Parameter(ParameterSetName = "Rendering by filter, Item from ID")]
        public string DataSource { get; set; }

        [Parameter(ParameterSetName = "Rendering by filter, Item from Pipeline")]
        [Parameter(ParameterSetName = "Rendering by filter, Item from Path")]
        [Parameter(ParameterSetName = "Rendering by filter, Item from ID")]
        public Item Rendering { get; set; }

        [Parameter(ParameterSetName = "Rendering by filter, Item from Pipeline")]
        [Parameter(ParameterSetName = "Rendering by filter, Item from Path")]
        [Parameter(ParameterSetName = "Rendering by filter, Item from ID")]
        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        [Parameter(ParameterSetName = "Rendering by filter, Item from Pipeline")]
        [Parameter(ParameterSetName = "Rendering by filter, Item from Path")]
        [Parameter(ParameterSetName = "Rendering by filter, Item from ID")]
        public string PlaceHolder { get; set; }

        [Parameter(ParameterSetName = "Rendering by filter, Item from Pipeline")]
        [Parameter(ParameterSetName = "Rendering by filter, Item from Path")]
        [Parameter(ParameterSetName = "Rendering by filter, Item from ID")]
        public Hashtable Parameter { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Rendering by instance, Item from Pipeline")]
        [Parameter(Mandatory = true, ParameterSetName = "Rendering by instance, Item from Path")]
        [Parameter(Mandatory = true, ParameterSetName = "Rendering by instance, Item from ID")]
        public RenderingDefinition Instance { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Rendering by unique ID, Item from Pipeline")]
        [Parameter(Mandatory = true, ParameterSetName = "Rendering by unique ID, Item from Path")]
        [Parameter(Mandatory = true, ParameterSetName = "Rendering by unique ID, Item from ID")]
        public string UniqueId { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (Parameter != null)
            {
                paramPatterns = new Dictionary<string, WildcardPattern>();
                foreach (var key in Parameter.Keys)
                {
                    var wildcardPattern = WildcardUtils.GetWildcardPattern(Parameter[key].ToString());
                    paramPatterns.Add(key.ToString(), wildcardPattern);
                }
            }
        }

        protected override void ProcessLayout(Item item, LayoutDefinition layout, DeviceDefinition device)
        {
            if (device == null || device.Renderings == null)
            {
                return;
            }

            var renderings = device.Renderings.Cast<RenderingDefinition>();

            if (Instance != null)
            {
                renderings = new[] {Instance};
            }
            else if (!string.IsNullOrEmpty(UniqueId))
            {
                renderings =
                    renderings.Where(r => string.Equals(r.UniqueId, UniqueId, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                if (Rendering != null)
                {
                    renderings = renderings.Where(r => r.ItemID == Rendering.ID.ToString());
                }

                if (!string.IsNullOrEmpty(DataSource))
                {
                    renderings = WildcardFilter(DataSource, renderings, r => r.Datasource);
                }

                if (!string.IsNullOrEmpty(PlaceHolder))
                {
                    renderings = WildcardFilter(PlaceHolder, renderings, r => r.Placeholder);
                }

                if (Index > -1)
                {
                    renderings = renderings.Skip(index).Take(1);
                }
            }

            if (paramPatterns != null)
            {
                var paramFilteredRenderings = new List<RenderingDefinition>();
                foreach (var rendering in renderings)
                {
                    if (string.IsNullOrEmpty(rendering.Parameters))
                    {
                        continue;
                    }
                    var parsedParams = new UrlString(rendering.Parameters);
                    var match = true;
                    foreach (var param in paramPatterns)
                    {
                        if (!parsedParams.Parameters.AllKeys.Contains(param.Key, StringComparer.OrdinalIgnoreCase))
                        {
                            match = false;
                            break;
                        }
                        if (!param.Value.IsMatch(parsedParams.Parameters[param.Key]))
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        paramFilteredRenderings.Add(rendering);
                    }
                }
                renderings = paramFilteredRenderings;
            }
            ProcessRenderings(item, layout, device, renderings);
        }

        protected abstract void ProcessRenderings(Item item, LayoutDefinition layout, DeviceDefinition device,
            IEnumerable<RenderingDefinition> renderings);
    }
}