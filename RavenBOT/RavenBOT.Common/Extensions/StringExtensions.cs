using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;

namespace RavenBOT.Common
{
    public static partial class Extensions
    {
        public static string FixLength(this string value, int length = 1023)
        {
            if (value.Length > length)
            {
                value = value.Substring(0, length - 3) + "...";
            }

            return value;
        }

        public static Embed QuickEmbed(this string message, Discord.Color? color = null)
        {
            return new EmbedBuilder
            {
                Description = message.FixLength(2047),
                    Color = color ?? Discord.Color.Blue
            }.Build();
        }

        public static IEnumerable<IEnumerable<T>> SplitList<T>(this IEnumerable<T> list, int groupSize = 30)
        {
            var splitList = new List<IEnumerable<T>>();
            for (var i = 0; i < list.Count(); i += groupSize)
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

        public static IDictionary<String, Int32> ConvertEnumToDictionary<K>()
        {
            //Ensure that the base type is actually an enum
            if (typeof(K).BaseType != typeof(Enum))
            {
                throw new InvalidCastException();
            }

            return Enum.GetValues(typeof(K)).Cast<Int32>().ToDictionary(currentItem => Enum.GetName(typeof(K), currentItem));
        }
    }
}