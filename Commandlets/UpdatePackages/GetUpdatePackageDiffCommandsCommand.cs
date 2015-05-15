using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Management.Automation;
using System.Xml;
using Cognifide.PowerShell.Commandlets.Packages;
using log4net.Config;
using Sitecore.Update.Commands;
using Sitecore.Update.Configuration;
using Sitecore.Update.Data;
using Sitecore.Update.Interfaces;

namespace Cognifide.PowerShell.Commandlets.UpdatePackages
{
    [Cmdlet(VerbsCommon.Get, "UpdatePackageDiff")]
    [OutputType(typeof (ICommand))]
    public class GetUpdatePackageDiffCommand : BasePackageCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string SourcePath { get; set; }

        [Parameter(Position = 0)]
        public string TargetPath { get; set; }

        protected override void ProcessRecord()
        {
            // Use default logger
            XmlConfigurator.Configure((XmlElement) ConfigurationManager.GetSection("log4net"));

            PerformInstallAction("admin",
                () =>
                {
                    var targetManager = Factory.Instance.GetSourceDataManager();
                    var sourceManager = Factory.Instance.GetTargetDataManager();

                    sourceManager.SerializationPath = SourcePath;
                    targetManager.SerializationPath = TargetPath;

                    var sourceDataIterator = sourceManager.ItemIterator;
                    var targetDataIterator = targetManager.ItemIterator;

                    var engine = new DataEngine();

                    var commands = new List<ICommand>();
                    commands.AddRange(GenerateDiff(sourceDataIterator, targetDataIterator));
                    //if an item is found to be deleted AND added, we can be sure it's a move
                    var deleteCommands = commands.OfType<DeleteItemCommand>();
                    var shouldBeUpdateCommands =
                        commands.OfType<AddItemCommand>()
                            .Select(a => new
                            {
                                Added = a,
                                Deleted = deleteCommands.FirstOrDefault(d => d.ItemID == a.ItemID)
                            }).Where(u => u.Deleted != null).ToList();
                    foreach (var command in shouldBeUpdateCommands)
                    {
                        commands.AddRange(command.Deleted.GenerateUpdateCommand(command.Added));
                        commands.Remove(command.Added);
                        commands.Remove(command.Deleted);
                        //now, this one is an assumption, but would go wrong without the assumption anyway: this assumption is in fact safer
                        //if the itempath of a delete command starts with this delete command, it will be moved along to the new node, not deleted, just leave it alone
                        commands.RemoveAll(
                            c =>
                                c is DeleteItemCommand &&
                                ((DeleteItemCommand) c).ItemPath.StartsWith(command.Deleted.ItemPath));
                    }

                    engine.ProcessCommands(ref commands);
                    WriteObject(commands, true);
                });
        }

        protected static IList<ICommand> GenerateDiff(IDataIterator sourceIterator, IDataIterator targetIterator)
        {
            var commands = new List<ICommand>();
            var sourceDataItem = sourceIterator.Next();
            var targetDataItem = targetIterator.Next();

            while (sourceDataItem != null || targetDataItem != null)
            {
                var compareResult = Compare(sourceDataItem, targetDataItem);
                commands.AddRange((sourceDataItem ?? targetDataItem).GenerateDiff(sourceDataItem, targetDataItem,
                    compareResult));
                if (compareResult < 0)
                {
                    sourceDataItem = sourceIterator.Next();
                }
                else
                {
                    if (compareResult > 0)
                    {
                        targetDataItem = targetIterator.Next();
                    }
                    else
                    {
                        if (compareResult == 0)
                        {
                            targetDataItem = targetIterator.Next();
                            sourceDataItem = sourceIterator.Next();
                        }
                    }
                }
            }
            return commands;
        }

        protected static int Compare(IDataItem sourceItem, IDataItem targetItem)
        {
            if (sourceItem == null && targetItem == null)
            {
                return 0;
            }

            if (sourceItem == null)
            {
                return 1;
            }

            if (targetItem == null)
            {
                return -1;
            }

            return sourceItem.CompareTo(targetItem);
        }
    }
}