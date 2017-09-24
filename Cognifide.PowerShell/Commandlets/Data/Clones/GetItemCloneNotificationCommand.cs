using System.Management.Automation;
using Sitecore.Data.Clones;

namespace Cognifide.PowerShell.Commandlets.Data.Clones
{
    [Cmdlet(VerbsCommon.Get, "ItemCloneNotification", SupportsShouldProcess = true)]
    [OutputType(typeof(Notification), ParameterSetName = new[] { "Item from Pipeline", "Item from Path", "Item from ID" })]
    public class GetItemCloneNotificationCommand : BaseItemCloneNotificationCommand
    {
        protected override void ProcessNotification(Notification notification)
        {
            WriteObject(notification);
        }
    }
}