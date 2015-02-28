using System;
using System.Data;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Security;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Exceptions;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Core.Extensions
{
    public static class CmdletExtensions
    {
        public static bool CanChangeReadOnly(this Cmdlet command, Item item)
        {
            if (item.Fields[FieldIDs.ReadOnly] == null)
            {
                var error = String.Format("Item '{0}' does not have a ReadOnly field.", item.Name);
                command.WriteError(new ErrorRecord(new PSInvalidOperationException(error), error,
                    ErrorCategory.InvalidData,
                    item));
                return false;
            }

            if (!item.Fields[FieldIDs.ReadOnly].CanWrite)
            {
                var error = String.Format("Cannot modify item '{0}' because the ReadOnly field cannot be written.",
                    item.Name);
                command.WriteError(new ErrorRecord(new SecurityException(error), error, ErrorCategory.PermissionDenied,
                    item));
                return false;
            }

            return true;
        }

        public static bool CanWrite(this Cmdlet command, Item item)
        {
            if (item.Access.CanWrite()) return true;

            var error = String.Format("Cannot modify item '{0}' because the item cannot be written.", item.Name);
            command.WriteError(new ErrorRecord(new SecurityException(error), error, ErrorCategory.PermissionDenied, item));
            return false;
        }

        public static bool CanChangeLock(this Cmdlet command, Item item)
        {
            if (item.Locking.HasLock() || item.Locking.CanUnlock() ||
                (!item.Locking.IsLocked() && item.Locking.CanLock())) return true;

            var error = String.Format("Cannot modify item '{0}' because it is locked by '{1}'.", item.Name,
                item.Locking.GetOwner());
            command.WriteError(new ErrorRecord(new SecurityException(error), error, ErrorCategory.PermissionDenied,
                item));
            return false;
        }

        public static bool CanFindAccount(this Cmdlet command, AccountIdentity account, AccountType accountType)
        {
            var name = account.Name;
            var error = String.Format("Cannot find an account with identity '{0}'.", name);

            if (accountType == AccountType.Role && !Role.Exists(name))
            {
                command.WriteError(new ErrorRecord(new ObjectNotFoundException(error), error,
                    ErrorCategory.ObjectNotFound, account));
                return false;
            }
            if (accountType == AccountType.User && !User.Exists(name))
            {
                command.WriteError(new ErrorRecord(new ObjectNotFoundException(error), error,
                    ErrorCategory.ObjectNotFound, account));
                return false;
            }

            return true;
        }
    }
}