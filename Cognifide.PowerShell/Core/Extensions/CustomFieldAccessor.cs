using System.Dynamic;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Core.Extensions
{
    public class CustomFieldAccessor : DynamicObject
    {
        private Item item;

        public CustomFieldAccessor(Item item)
        {
            this.item = item;
        }

        public override bool TryGetMember(
            GetMemberBinder binder, out object result)
        {
            string name = binder.Name.ToLower();
            var field = item.Fields[name];
            result = field != null ? FieldTypeManager.GetField(item.Fields[name]) : null;
            return field != null;
        }

        public override bool TrySetMember(
            SetMemberBinder binder, object value)
        {
            ItemShellExtensions.ModifyProperty(item, binder.Name, value);
            return true;
        }
    }
}