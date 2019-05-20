using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RavenBOT.Modules.AutoMod.Models.Moderation;
using CaptchaGen.NetCore;
using System.IO;

namespace RavenBOT.Modules.AutoMod.Methods
{
    public partial class ModerationService
    {
        public async Task PerformCaptchaAction(ModerationConfig.Captcha.Action actionType, SocketGuildUser user)
        {
            if (actionType == ModerationConfig.Captcha.Action.None)
            {
                return;
            }
            
            if (actionType == ModerationConfig.Captcha.Action.Kick)
            {
                await user.KickAsync("Failed to pass captcha verification");
                return;
            }

            if (actionType == ModerationConfig.Captcha.Action.Ban)
            {
                await user.BanAsync(7, "Failed to pass captcha verification");
                return;
            }
        }

        public async Task<IRole> GetOrCreateCaptchaRole(ModerationConfig config, SocketGuild guild)
        {
            IRole role;
            if (config.CaptchaSettings.CaptchaTempRole == 0)
            {
                role = await guild.CreateRoleAsync("CaptchaTempRole");
                config.CaptchaSettings.CaptchaTempRole = role.Id;
                SaveModerationConfig(config);
            }
            else
            {
                role = guild.GetRole(config.CaptchaSettings.CaptchaTempRole);
                if (role == null)
                {
                    role = await guild.CreateRoleAsync("CaptchaTempRole");
                    config.CaptchaSettings.CaptchaTempRole = role.Id;
                    SaveModerationConfig(config);
                }

                if (role.Permissions.SendMessages || role.Permissions.AddReactions || role.Permissions.Connect || role.Permissions.Speak)
                {
                    await role.ModifyAsync(x =>
                    {
                        x.Permissions = new GuildPermissions(sendMessages: false, addReactions: false, connect: false, speak: false);
                    });
                }
            }

            foreach (var channel in guild.Channels)
            {
                if (channel.PermissionOverwrites.All(x => x.TargetId != role.Id))
                {
                    var _ = Task.Run(async () => await channel.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny, addReactions: PermValue.Deny, connect: PermValue.Deny, speak: PermValue.Deny)));
                }
            }

            return role;
        }

        public CaptchaUser GetCaptchaUser(ulong userId, ulong guildId)
        {
            var captchaDoc = Database.Load<CaptchaUser>(CaptchaUser.DocumentName(userId, guildId));
            return captchaDoc;
        }
        private CaptchaUser GetOrCreateCaptchaUser(ulong userId, ulong guildId)
        {
            var captchaDoc = Database.Load<CaptchaUser>(CaptchaUser.DocumentName(userId, guildId));

            if (captchaDoc == null)
            {
                captchaDoc = new CaptchaUser(userId, guildId, GenerateCaptcha());
                Database.Store(captchaDoc, CaptchaUser.DocumentName(userId, guildId));
            }

            return captchaDoc;
        }

        public void SaveCaptchaUser(CaptchaUser user)
        {
            Database.Store(user, CaptchaUser.DocumentName(user.UserId, user.GuildId));
        }

        public async Task ChannelCreated(SocketChannel channel)
        {
            if (channel is SocketGuildChannel gChannel)
            {
                var config = GetModerationConfig(gChannel.Guild.Id);
                if (config.UseCaptcha)
                {
                    //The GetOrCreateCaptchaRole method automatically updates all channels in a server.
                    //The only reason this method is needed is in the case that a channel is created and a user still has the
                    //Captcha temp role.
                    //The Channel will need the permissions to be updated.
                    var _ = await GetOrCreateCaptchaRole(config, gChannel.Guild);
                }               
            }
        }

        public async Task UserJoined(SocketGuildUser user)
        {
            var config = GetModerationConfig(user.Guild.Id);
            if (config.UseCaptcha)
            {
                var captchaDoc = GetOrCreateCaptchaUser(user.Id, user.Guild.Id);

                if (captchaDoc.Passed)
                {
                    return;
                }

                var role = await GetOrCreateCaptchaRole(config, user.Guild);

                await user.AddRoleAsync(role);

                if (captchaDoc.FailureCount >= config.CaptchaSettings.MaxFailures)
                {
                    return;
                }

                var channel = await user.GetOrCreateDMChannelAsync();
                if (channel == null)
                {
                    return;
                }


                Stream imageStream = CaptchaGen.NetCore.ImageFactory.BuildImage(captchaDoc.Captcha, 100, 150, 25, 10, ImageFormatType.Jpeg);
                await user.SendFileAsync(imageStream, "captcha.jpg", $"Please run the Verify command in order to speak in {user.Guild.Name}. ie. `lithium.moderation.Verify {user.Guild.Id} <code>`");
            }
        }
        public string GenerateCaptcha()
        {
            //Removed I, lowercase, 0 and O
            var characters = "ABCDEFGHJKLMNPQRSTUVWXYZ123456789".ToCharArray();
            var captchaString = new StringBuilder();
            for (int i = 0; i < 6; i++)
            {
                var character = characters.OrderByDescending(x => Random.Next()).First();
                captchaString.Append(character);
            }

            return captchaString.ToString();
        }

        public class CaptchaUser
        {
            public static string DocumentName(ulong userId, ulong guildId)
            {
                return $"CaptchaUser-{userId}-{guildId}";
            }

            public CaptchaUser(){}

            public CaptchaUser(ulong userId, ulong guildId, string captcha)
            {
                UserId = userId;
                GuildId = guildId;

                Captcha = captcha;
            }

            public ulong UserId {get;set;}
            public ulong GuildId {get;set;}

            public string Captcha {get;set;}

            public bool Passed {get;set;} = false;
            public int FailureCount {get;set;} = 0;
        }
    }
}