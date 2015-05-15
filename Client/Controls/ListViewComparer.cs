using System;
using System.Collections.Generic;

namespace Cognifide.PowerShell.Client.Controls
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

        public int Compare(String str1, String str2)
        {
            DateTime dateTime1, dateTime2;
            if (DateTime.TryParse(str1, out dateTime1) && DateTime.TryParse(str2, out dateTime2))
            {
                return DateTime.Compare(dateTime1, dateTime2);
            }

            int int1, int2;
            if (int.TryParse(str1, out int1) && int.TryParse(str2, out int2))
            {
                if (int1 < int2)
                    return -1;
                return int1 > int2 ? 1 : 0;
            }

            return String.Compare(str1, str2, StringComparison.Ordinal);
        }
    }
}