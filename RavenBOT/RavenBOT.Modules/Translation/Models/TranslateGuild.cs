using System.Collections.Generic;
using RavenBOT.Modules.Translation.Models;

namespace RavenBOT.Modules.Translation.Models
{
    public class TranslateGuild
    {
        public static string DocumentName(ulong guildId)
        {
            return $"TranslateGuild-{guildId}";
        }

        public TranslateGuild(ulong guildId)
        {
            GuildId = guildId;
        }

        public TranslateGuild() { }

        public ulong GuildId { get; set; }

        public bool ReactionTranslations { get; set; } = false;
        public bool DirectMessageTranslations { get; set; } = false;
        public List<LanguageMap.TranslationSet> CustomPairs { get; set; } = new List<LanguageMap.TranslationSet>();

        public List<ulong> WhitelistRoles { get; set; } = new List<ulong>();
    }
}