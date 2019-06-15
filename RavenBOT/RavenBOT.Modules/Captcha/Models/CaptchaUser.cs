namespace RavenBOT.Modules.Captcha.Models
{
    public class CaptchaUser
    {
        public static string DocumentName(ulong userId, ulong guildId)
        {
            return $"CaptchaUser-{userId}-{guildId}";
        }

        public CaptchaUser() { }

        public CaptchaUser(ulong userId, ulong guildId, string captcha)
        {
            UserId = userId;
            GuildId = guildId;

            Captcha = captcha;
        }

        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }

        public string Captcha { get; set; }

        public bool Passed { get; set; } = false;
        public int FailureCount { get; set; } = 0;
    }
}