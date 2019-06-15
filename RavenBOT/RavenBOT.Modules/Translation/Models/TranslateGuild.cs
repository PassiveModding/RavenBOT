using System.Collections.Generic;
using static RavenBOT.Modules.Translation.Models.LanguageMap;

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
        public List<TranslationSet> CustomPairs { get; set; } = new List<TranslationSet>();

        public List<ulong> WhitelistRoles { get; set; } = new List<ulong>();
    }
}