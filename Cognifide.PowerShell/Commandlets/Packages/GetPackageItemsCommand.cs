using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data.Items;
using Sitecore.Install;
using Sitecore.Install.Framework;
using Sitecore.Install.Items;
using Sitecore.Install.Serialization;
using Sitecore.Install.Zip;
using Sitecore.IO;
using Sitecore.Shell.Applications.ContentEditor;
using Constants = Sitecore.Install.Constants;

namespace Cognifide.PowerShell.Commandlets.Packages
{
	[Cmdlet(VerbsCommon.Get, "PackageItems", SupportsShouldProcess = true)]
	public class GetPackageItemsCommand : BaseCommand
	{
		[Parameter(Position = 1, ValueFromPipeline = true, Mandatory = true)]
		[ValidateNotNullOrEmpty]
		public PackageProject Project { get; set; }

        [Parameter]
		public SwitchParameter SkipDuplicates { get; set; }

		private class ItemSinkHelper : BaseSink<PackageEntry>
		{
			private readonly bool _removeDuplicates;

			private readonly HashSet<Item> _itemSet;
			private readonly List<Item> _itemList;

			public IEnumerable<Item> Items => _removeDuplicates ? _itemSet.AsEnumerable() : _itemList.AsEnumerable();

			private void AddItem(Item item)
			{
				if (_removeDuplicates)
					_itemSet.Add(item);
				else
					_itemList.Add(item);
			}

			public ItemSinkHelper(bool removeDuplicates)
			{
				_removeDuplicates = removeDuplicates;

				if (_removeDuplicates)
					_itemSet = new HashSet<Item>(ItemEqualityComparer.Instance);
				else
					_itemList = new List<Item>();
			}

			public override void Put(PackageEntry entry)
			{
				var itemReference = ItemKeyUtils.GetReference(entry.Key);

				var item = itemReference?.GetItem();

				if (item != null)
					AddItem(item);
			}
		}

		protected override void ProcessRecord()
		{
			ItemSinkHelper itemSinkHelper = new ItemSinkHelper(SkipDuplicates.IsPresent);
			itemSinkHelper.Initialize(Installer.CreatePreviewContext());

			foreach (var source in Project.Sources)
				source.Populate(itemSinkHelper);

			foreach (var item in itemSinkHelper.Items)
				WriteItem(item);
		}
	}
}