// CS0618: Manager is obsolete. Suppressed intentionally -- this class exists to wrap the
// deprecated Sitecore.Data.Serialization.Manager API for Sitecore 9.2 backwards compatibility.
// See https://github.com/SitecorePowerShell/Console/issues/1433
#pragma warning disable CS0618

using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Security.Accounts;
using Sitecore.Security.Serialization;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.VersionSpecific.Services
{
    public class SpeSerializationManager : ISerializationManager
    {
        public void DumpItem(Item item)
        {
            Manager.DumpItem(item);
        }

        public void DumpItem(string filePath, Item item)
        {
            Manager.DumpItem(filePath, item);
        }

        public void LoadItem(string path, LoadOptions options)
        {
            Manager.LoadItem(path, options);
        }

        public void LoadTree(string path, LoadOptions options)
        {
            Manager.LoadTree(path, options);
        }

        public void DumpUser(string userName)
        {
            Manager.DumpUser(userName);
        }

        public void DumpUser(string filePath, User user)
        {
            var userReference = new UserReference(user.Name);
            Manager.DumpUser(filePath, userReference.User);
        }

        public void LoadUser(string filePath)
        {
            Manager.LoadUser(filePath);
        }

        public void DumpRole(string roleName)
        {
            Manager.DumpRole(roleName);
        }

        public void DumpRole(string filePath, Role role)
        {
            Manager.DumpRole(filePath, role);
        }

        public void LoadRole(string filePath)
        {
            Manager.LoadRole(filePath);
        }
    }
}
