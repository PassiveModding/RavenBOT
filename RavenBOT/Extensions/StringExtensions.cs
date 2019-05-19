using System;
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

        public static string GetReadableLength(this TimeSpan length)
        {
            int days = (int) length.TotalDays;
            int hours = (int) length.TotalHours - days * 24;
            int minutes = (int) length.TotalMinutes - days * 24 * 60 - hours * 60;
            int seconds = (int) length.TotalSeconds - days * 24 * 60 * 60 - hours * 60 * 60 - minutes * 60;

            return $"{(days > 0 ? $"{days} Day(s) " : "")}{(hours > 0 ? $"{hours} Hour(s) " : "")}{(minutes > 0 ? $"{minutes} Minute(s) " : "")}{(seconds > 0 ? $"{seconds} Second(s)" : "")}";
        }
    }
}
