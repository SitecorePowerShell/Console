using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Management.Automation;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace Spe.Core.Extensions
{
    public class CustomFieldAccessor : DynamicObject, IEnumerable<PSObject>
    {
        private readonly Item _item;

        public CustomFieldAccessor(Item item)
        {
            _item = item;
        }

        public IEnumerator<PSObject> GetEnumerator()
        {
            foreach (Field itemField in _item.Fields)
            {
                if (itemField == null) continue;

                var field = FieldTypeManager.GetField(itemField);
                if (field == null) continue;

                var extendedField = new PSObject(field);
                extendedField.Properties.Add(new PSNoteProperty("Name", itemField.Name));

                yield return extendedField;
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