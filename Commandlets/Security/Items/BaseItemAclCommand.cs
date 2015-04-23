using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Management.Automation;
using System.Xml;
using Sitecore.Configuration;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Exceptions;
using Sitecore.Security.AccessControl;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security.Items
{
    public abstract class BaseItemAclCommand : BaseItemCommand
    {
        public static readonly string[] WellKnownRights =
            Factory.GetConfigNode("accessRights/rights")
                .ChildNodes
                .Cast<XmlNode>()
                .Select(node => node.Attributes["name"].Value)
                .ToArray();

        [Alias("User")]
        [Parameter(ParameterSetName = "Account ID, Item from Path", Mandatory = true)]
        [Parameter(ParameterSetName = "Account ID, Item from ID", Mandatory = true)]
        [Parameter(ParameterSetName = "Account ID, Item from Pipeline", Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public virtual AccountIdentity Identity { get; set; }

        [Parameter(ParameterSetName = "Account Filter, Item from Path", Mandatory = true)]
        [Parameter(ParameterSetName = "Account Filter, Item from ID", Mandatory = true)]
        [Parameter(ParameterSetName = "Account Filter, Item from Pipeline", Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public virtual string Filter { get; set; }

        [Parameter(ParameterSetName = "Item from Pipeline", Mandatory = true, ValueFromPipeline = true)]
        [Parameter(ParameterSetName = "Account ID, Item from Pipeline", Mandatory = true, ValueFromPipeline = true)]
        [Parameter(ParameterSetName = "Account Filter, Item from Pipeline", Mandatory = true, ValueFromPipeline = true)]
        public override Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path", Mandatory = true)]
        [Parameter(ParameterSetName = "Account ID, Item from Path", Mandatory = true)]
        [Parameter(ParameterSetName = "Account Filter, Item from Path", Mandatory = true)]
        [Alias("FullName", "FileName")]
        public override string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID", Mandatory = true)]
        [Parameter(ParameterSetName = "Account ID, Item from ID", Mandatory = true)]
        [Parameter(ParameterSetName = "Account Filter, Item from ID", Mandatory = true)]
        public override string Id { get; set; }

        [Parameter(ParameterSetName = "Account ID, Item from ID")]
        [Parameter(ParameterSetName = "Account Filter, Item from ID")]
        public override Database Database { get; set; }

        [Alias("Languages")]
        [Parameter]
        public override string[] Language { get; set; }

    }
}