using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Core.Extensions
{
    public class CustomFieldAccessor : DynamicObject
    {
        private readonly Item _item;

        public CustomFieldAccessor(Item item)
        {
            _item = item;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var name = binder.Name.ToLower();
            var field = _item.Fields[name];
            result = field != null ? FieldTypeManager.GetField(_item.Fields[name]) : null;
            return field != null;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            ItemShellExtensions.ModifyProperty(_item, binder.Name, value);
            return true;
        }
    }
}