using System;

namespace Cognifide.PowerShell.Core.Validation
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