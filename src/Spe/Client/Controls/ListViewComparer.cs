using System;
using System.Collections.Generic;
using System.Globalization;

namespace Spe.Client.Controls
{
    public class ListViewComparer : IComparer<string>
    {
        private static ListViewComparer instance;

        private ListViewComparer()
        {
        }

        public static ListViewComparer Instance
        {
            get { return instance ?? (instance = new ListViewComparer()); }
        }

        public int Compare(string str1, string str2)
        {
            if (DateTime.TryParse(str1, out var dateTime1) && DateTime.TryParse(str2, out var dateTime2))
            {
                return DateTime.Compare(dateTime1, dateTime2);
            }

            if (double.TryParse(str1, NumberStyles.AllowCurrencySymbol | NumberStyles.AllowDecimalPoint, NumberFormatInfo.CurrentInfo, out var dec1)
                && double.TryParse(str2, NumberStyles.AllowCurrencySymbol | NumberStyles.AllowDecimalPoint, NumberFormatInfo.CurrentInfo, out var dec2))
            {
                if (dec1 < dec2)
                    return -1;
                return dec1 > dec2 ? 1 : 0;
            }

            return string.Compare(str1, str2, StringComparison.Ordinal);
        }
    }
}