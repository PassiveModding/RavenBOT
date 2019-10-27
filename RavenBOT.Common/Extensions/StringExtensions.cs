using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                //yield return list.Skip(i).Take(groupSize);
            }

            return splitList;
        }


        public static IEnumerable<IEnumerable<T>> SplitList<T>(this IEnumerable<T> list, Func<T, int> sumComparator, int maxGroupSum)
        {
            var subList = new List<T>();
            int currentSum = 0;

            foreach (var item in list)
            {
                //Get the size of the current item.
                var addedValue = sumComparator(item);

                //Ensure that the current item will fit in a group
                if (addedValue > maxGroupSum)
                {
                    //TODO: add options to skip fields that exceed the length or add them as a solo group rather than just error out
                    throw new InvalidOperationException("A fields value is greater than the maximum group value size.");
                }

                //Add group to splitlist if the new item will exceed the given size.
                if (currentSum + addedValue > maxGroupSum)
                {
                    //splitList.Append(subList);
                    yield return subList;
                    //Clear the current sum and the subList
                    currentSum = 0;
                    subList = new List<T>();
                }

                subList.Add(item);
                currentSum += addedValue;
            }

            //Return any remaining elements
            if (subList.Count != 0)
            {
                yield return subList;
            }
        }

        public static string GetReadableLength(this TimeSpan length)
        {
            int days = (int)length.TotalDays;
            int hours = (int)length.TotalHours - days * 24;
            int minutes = (int)length.TotalMinutes - days * 24 * 60 - hours * 60;
            int seconds = (int)length.TotalSeconds - days * 24 * 60 * 60 - hours * 60 * 60 - minutes * 60;

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

        public static List<Tuple<string, K>> GetEnumNameValues<K>()
        {
            //Ensure that the base type is actually an enum
            if (typeof(K).BaseType != typeof(Enum))
            {
                throw new InvalidCastException();
            }

            return Enum.GetNames(typeof(K)).Select(x => new Tuple<string, K>(x, (K)Enum.Parse(typeof(K), x))).ToList();
            //return Enum.GetValues(typeof(K)).Cast<Int32>().ToDictionary(currentItem => Enum.GetName(typeof(K), currentItem));
        }

        public static string[] EnumNames<K>()
        {
            //Ensure that the base type is actually an enum
            if (typeof(K).BaseType != typeof(Enum))
            {
                throw new InvalidCastException();
            }

            return Enum.GetNames(typeof(K));
        }
    }
}