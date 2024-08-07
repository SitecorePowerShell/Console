﻿using System.Linq;
using System.Management.Automation;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Links;
using Spe.Core.Extensions;
using Spe.Core.Validation;

namespace Spe.Commands.Data
{
    [Cmdlet(VerbsCommon.Get, "ItemReference")]
    [OutputType(typeof (Item),
        ParameterSetName =
            new[] {"Item from Pipeline, return Item", "Item from Path, return Item", "Item from ID, return Item"})]
    [OutputType(typeof (ItemLink),
        ParameterSetName =
            new[]
            {"Item from Pipeline, return ItemLink", "Item from Path, return ItemLink", "Item from ID, return ItemLink"})
    ]
    public class GetItemReferenceCommand : BaseItemCommand
    {
        [Parameter(ValueFromPipeline = true, ParameterSetName = "Item from Pipeline, return Item", Mandatory = true)]
        [Parameter(ValueFromPipeline = true, ParameterSetName = "Item from Pipeline, return ItemLink", Mandatory = true)
        ]
        public override Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path, return Item", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from Path, return ItemLink", Mandatory = true)]
        [Alias("FullName", "FileName")]
        public override string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID, return Item", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from ID, return ItemLink", Mandatory = true)]
        public override string Id { get; set; }

        [AutocompleteSet(nameof(Databases))]
        [Parameter(ParameterSetName = "Item from ID, return Item")]
        [Parameter(ParameterSetName = "Item from ID, return ItemLink")]
        public override string Database { get; set; }

        [Alias("Languages")]
        [AutocompleteSet(nameof(Cultures))]
        [Parameter(ParameterSetName = "Item from Path, return Item")]
        [Parameter(ParameterSetName = "Item from ID, return Item")]
        [Parameter(ParameterSetName = "Item from Path, return ItemLink")]
        [Parameter(ParameterSetName = "Item from ID, return ItemLink")]
        public override string[] Language { get; set; }

        [Alias("Deep", "Recursive", "WithStandardValues", "WithFallback")]
        [Parameter(ParameterSetName = "Item from Path, return Item")]
        [Parameter(ParameterSetName = "Item from ID, return Item")]
        [Parameter(ParameterSetName = "Item from Path, return ItemLink")]
        [Parameter(ParameterSetName = "Item from ID, return ItemLink")]
        [Parameter(ParameterSetName = "Item from Pipeline, return Item")]
        [Parameter(ParameterSetName = "Item from Pipeline, return ItemLink")]
        public SwitchParameter Recurse { get; set; }

        [Parameter(ParameterSetName = "Item from Path, return ItemLink", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from ID, return ItemLink", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from Pipeline, return ItemLink", Mandatory = true)]
        public SwitchParameter ItemLink { get; set; }

        protected override void ProcessItem(Item linkedItem)
        {
            var linkDb = Globals.LinkDatabase;

            if (ItemLink)
            {
                linkDb
                    .GetItemReferences(linkedItem, Recurse)
                    .ToList()
                    .ForEach(WriteObject);
            }
            else
            {
                linkDb.GetItemReferences(linkedItem, Recurse)
                    .Select(link => link.GetTargetItem())
                    .Distinct(ItemEqualityComparer.Instance)
                    .ToList()
                    .ForEach(WriteItem);
            }
        }
    }
}