using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Security.Accounts;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface ISerializationManager
    {
        void DumpItem(Item item);
        void DumpItem(string filePath, Item item);
        void LoadItem(string path, LoadOptions options);
        void LoadTree(string path, LoadOptions options);
        void DumpUser(string userName);
        void DumpUser(string filePath, User user);
        void LoadUser(string filePath);
        void DumpRole(string roleName);
        void DumpRole(string filePath, Role role);
        void LoadRole(string filePath);
    }
}
