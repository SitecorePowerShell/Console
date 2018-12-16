using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Core.Extensions
{
    public class CustomFieldAccessor : DynamicObject, IEnumerable<CustomField>
    {
        private readonly Item _item;

        public CustomFieldAccessor(Item item)
        {
            _item = item;
        }

        public IEnumerator<CustomField> GetEnumerator()
        {
            foreach (Field itemField in _item.Fields)
            {
                if (itemField == null) continue;

                var field = FieldTypeManager.GetField(itemField);
                if (field != null)
                {
                    yield return field;
                }
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var name = binder.Name.ToLower();
            var field = _item.Fields[name];
            result = field != null ? FieldTypeManager.GetField(field) : null;
            return field != null;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            ItemShellExtensions.ModifyProperty(_item, binder.Name, value);
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}