using System;
using Discord;

namespace RavenBOT.Discord.Context.Interactive.Paginator
{
    public class PaginatedAppearanceOptions
    {
        public static PaginatedAppearanceOptions Default = new PaginatedAppearanceOptions();
        public IEmote Back = new Emoji("◀");
        public bool DisplayInformationIcon = true;

        public IEmote First = new Emoji("⏮");

        public string FooterFormat = "Page {0}/{1}";
        public IEmote Info = new Emoji("ℹ");
        public string InformationText = "This is a paginator. React with the respective icons to change page.";
        public TimeSpan InfoTimeout = TimeSpan.FromSeconds(30);
        public IEmote Jump = new Emoji("🔢");

        public JumpDisplayOptions JumpDisplayOptions = JumpDisplayOptions.WithManageMessages;
        public IEmote Last = new Emoji("⏭");
        public IEmote Next = new Emoji("▶");
        public IEmote Stop = new Emoji("⏹");

        public TimeSpan? Timeout = TimeSpan.FromHours(1);
    }

    public enum JumpDisplayOptions
    {
        Never,
        WithManageMessages,
        Always
    }
}