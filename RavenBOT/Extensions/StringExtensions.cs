using System.Collections.Generic;
using System.Linq;

namespace RavenBOT.Extensions
{
    public static class StringExtensions
    {
        public static string FixLength(this string value, int length = 1023)
        {
            if (value.Length > length)
            {
                value = value.Substring(0, length - 3) + "...";
            }

            return value;
        }

        public static List<List<T>> SplitList<T>(this List<T> list, int groupSize = 30)
        {
            var splitList = new List<List<T>>();
            for (var i = 0; i < list.Count; i += groupSize)
            {
                splitList.Add(list.Skip(i).Take(groupSize).ToList());
            }

            return splitList;
        }
    }
}
