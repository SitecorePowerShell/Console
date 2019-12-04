using System;

namespace Spe.Core.Validation
{
    public class AutocompleteSetAttribute : Attribute
    {
        public string Values { get; private set; }

        public AutocompleteSetAttribute(string values)
        {
            Values = values;
        }
    }
}