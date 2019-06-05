using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RavenBOT.Extensions
{
    public static class StringExtensions
    {
        public static string FixGfycatUrl(this string original)
        {
            if (original.Contains("gfycat", StringComparison.InvariantCultureIgnoreCase))
            {
                if (original.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase))
                {
                    original = original.Replace(".mp4", ".gif", StringComparison.InvariantCultureIgnoreCase);
                }
                else if (original.EndsWith(".webm", StringComparison.InvariantCultureIgnoreCase))
                {
                    original = original.Replace(".webm", ".gif", StringComparison.InvariantCultureIgnoreCase);
                }

                if (original.Contains("giant.", StringComparison.InvariantCultureIgnoreCase))
                {
                    return original;
                }
                else if (original.Contains("zippy.", StringComparison.InvariantCultureIgnoreCase))
                {
                    return original;
                }
                else if (original.Contains("thumbs.", StringComparison.InvariantCultureIgnoreCase))
                {
                    //Fixes cdn and replaces mobile or size restricted tags.
                    original = original.Replace("thumbs.", "zippy.", StringComparison.InvariantCultureIgnoreCase);
                    if (original.Contains("-"))
                    {
                        int subStrIndex = original.IndexOf("-");
                        original = original.Substring(0, subStrIndex) + ".gif";
                    }
                    return original;
                }
                else
                {
                    var startIndex = original.IndexOf("gfycat", StringComparison.InvariantCultureIgnoreCase);
                    original = original.Substring(startIndex, original.Length - startIndex);
                    original = $"https://zippy.{original}.gif";
                    return original;
                }
            }

            return original;
        }

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

        public static string DecodeBase64(this string original)
        {
            try
            {
                byte[] data = Convert.FromBase64String(original);
                string decodedString = Encoding.UTF8.GetString(data);
                return decodedString;
            }
            catch
            {
                return original;
            }            
        }
    }
}
