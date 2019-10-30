using Discord;
using System;
using System.Linq;

namespace RavenBOT.Common.TypeReaders
{
    public static class EmojiExtensions
    {
        /// <summary>
        ///     Return a Unicode Emoji given a shorthand alias
        /// </summary>
        /// <param name="text">A shorthand alias for the emoji, e.g. :race_car:</param>
        /// <returns>A unicode emoji, for direct use in a reaction or message.</returns>
        public static Emoji FromText(string text)
        {
            text = text.Trim(':');
            var match = EmojiMap.Map.FirstOrDefault(x => x.Value == text);
            if (match.Key != null)
            {
                return new Emoji(match.Value);
            }

            throw new ArgumentException($"The given alias could not be matched to a Unicode Emoji, {match.Key} {match.Value}", nameof(text));
        }

        /// <summary>
        ///     Returns the shorthand alias for a given emoji.
        /// </summary>
        /// <param name="emoji">A unicode emoji.</param>
        /// <returns>A shorthand alias for the emoji, e.g. :race_car:</returns>
        /// <exception cref="System.Exception">If the emoji does not have a mapping, an exception will be thrown.</exception>
        public static string GetShorthand(this Emoji emoji)
        {
            var key = EmojiMap.Map.FirstOrDefault(x => x.Value == emoji.Name).Key;
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception($"Could not find an emoji with value '{emoji.Name}'");
            }

            return string.Concat(":", key, ":");
        }

        /// <summary>
        ///     Attempts to return the shorthand alias for a given emoji.
        /// </summary>
        /// <param name="emoji">A unicode emoji.</param>
        /// <param name="shorthand">A string reference, where the shorthand alias for the emoji will be placed.</param>
        /// <returns>True if the emoji was found, false if it was not.</returns>
        public static bool TryGetShorthand(this Emoji emoji, out string shorthand)
        {
            var key = EmojiMap.Map.FirstOrDefault(x => x.Value == emoji.Name).Key;
            if (string.IsNullOrEmpty(key))
            {
                shorthand = string.Empty;
                return false;
            }

            shorthand = string.Concat(":", key, ":");
            return true;
        }
    }
}