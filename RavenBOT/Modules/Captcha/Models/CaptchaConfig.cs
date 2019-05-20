using System;

namespace RavenBOT.Modules.Captcha.Models
{
    public class CaptchaConfig
    {
        public CaptchaConfig(ulong guildId)
        {
            GuildId = guildId;
        }

        public CaptchaConfig() {}

        public ulong GuildId {get;set;}


        public bool UseCaptcha {get;set;} = false;
        public ulong CaptchaTempRole {get; set;} = 0;

        public int MaxFailures {get; set;} = 3;

        public bool SetMaxFailures(int count)
        {
            if (count >= 1)
            {
                MaxFailures = count;
                return true;
            }

            return false;
        }

        public Action MaxFailuresAction {get;set;} = Action.Kick;
        public enum Action
        {
            Kick, 
            Ban,
            None
        }

        public static string DocumentName(ulong guildId)
        {
            return $"CaptcaConfig-{guildId}";
        }
    }
}