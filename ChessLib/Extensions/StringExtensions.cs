using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessLib.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveDoubleSpaces(this string str)
        {
            int idx;
            while ((idx = str.IndexOf("  ")) >= 0) {
                str = str.Replace("  ", " ");
            }
            return str;
        }
    }
}