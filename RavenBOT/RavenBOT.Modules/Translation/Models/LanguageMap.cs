using System.Collections.Generic;

namespace RavenBOT.Modules.Translation.Models
{
    public class LanguageMap
    {
        /// <summary>
        ///     Gets or sets the default map.
        /// </summary>
        public static List<TranslationSet> DefaultMap { get; set; } =
            new List<TranslationSet>
            {
                new TranslationSet { EmoteMatches = new List<string> { "🇦🇺", "🇺🇸", "🇪🇺", "🇳🇿" }, LanguageString = "en" },
                new TranslationSet { EmoteMatches = new List<string> { "🇭🇺" }, LanguageString = "hu" },
                new TranslationSet { EmoteMatches = new List<string> { "🇫🇷" }, LanguageString = "fr" },
                new TranslationSet { EmoteMatches = new List<string> { "🇫🇮" }, LanguageString = "fi" },
                new TranslationSet { EmoteMatches = new List<string> { "🇲🇽", "🇪🇸", "🇨🇴", "🇦🇷" }, LanguageString = "es" },
                new TranslationSet { EmoteMatches = new List<string> { "🇧🇷", "🇵🇹", "🇲🇿", "🇦🇴" }, LanguageString = "pt" },
                new TranslationSet { EmoteMatches = new List<string> { "🇩🇪", "🇦🇹", "🇨🇭", "🇧🇪", "🇱🇺", "🇱🇮" }, LanguageString = "de" },
                new TranslationSet { EmoteMatches = new List<string> { "🇮🇹", "🇨🇭", "🇸🇲", "🇻🇦" }, LanguageString = "it" },
                new TranslationSet { EmoteMatches = new List<string> { "🇨🇳", "🇸🇬", "🇹🇼" }, LanguageString = "zh" },
                new TranslationSet { EmoteMatches = new List<string> { "🇯🇵" }, LanguageString = "ja" }
            };

        public class TranslationSet
        {
            public List<string> EmoteMatches { get; set; } = new List<string>();
            public string LanguageString { get; set; }
        }
    }
}