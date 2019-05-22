using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using CaptchaGen.NetCore;
using System.IO;
using RavenBOT.Services.Database;
using System.Collections.Generic;
using RavenBOT.Modules.Captcha.Models;

namespace RavenBOT.Modules.Captcha.Methods
{
    public partial class CaptchaService
    {
        private IDatabase Database { get; }
        private DiscordShardedClient Client { get; }
        private Random Random { get; }

        public CaptchaService(IDatabase database, DiscordShardedClient client)
        {
            Database = database;

            Client = client;
            Client.ChannelCreated += ChannelCreated;
            Client.UserJoined += UserJoined;
            Random = new Random();
        }

        public async Task PerformCaptchaAction(CaptchaConfig.Action actionType, SocketGuildUser user)
        {
            if (actionType == CaptchaConfig.Action.None)
            {
                return;
            }
            
            if (actionType == CaptchaConfig.Action.Kick)
            {
                await user.KickAsync("Failed to pass captcha verification");
                return;
            }

            if (actionType == CaptchaConfig.Action.Ban)
            {
                await user.BanAsync(7, "Failed to pass captcha verification");
                return;
            }
        }

        public void SaveCaptchaConfig(CaptchaConfig config)
        {
            Database.Store(config, CaptchaConfig.DocumentName(config.GuildId));
        }

        public CaptchaConfig GetCaptchaConfig(ulong guildId)
        {

            //Try to load it from database, otherwise create a new one and store it.
            var document = Database.Load<CaptchaConfig>(CaptchaConfig.DocumentName(guildId));
            if (document == null)
            {
                document = new CaptchaConfig(guildId);
                Database.Store(document, CaptchaConfig.DocumentName(guildId));
            }

            return document;
        }

        public async Task<IRole> GetOrCreateCaptchaRole(CaptchaConfig config, SocketGuild guild)
        {
            IRole role;
            if (config.CaptchaTempRole == 0)
            {
                role = await guild.CreateRoleAsync("CaptchaTempRole");
                config.CaptchaTempRole = role.Id;
                SaveCaptchaConfig(config);
            }
            else
            {
                role = guild.GetRole(config.CaptchaTempRole);
                if (role == null)
                {
                    role = await guild.CreateRoleAsync("CaptchaTempRole");
                    config.CaptchaTempRole = role.Id;
                    SaveCaptchaConfig(config);
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
                var config = GetCaptchaConfig(gChannel.Guild.Id);
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
            var config = GetCaptchaConfig(user.Guild.Id);
            if (config.UseCaptcha)
            {
                var captchaDoc = GetOrCreateCaptchaUser(user.Id, user.Guild.Id);

                if (captchaDoc.Passed)
                {
                    return;
                }

                var role = await GetOrCreateCaptchaRole(config, user.Guild);

                await user.AddRoleAsync(role);

                if (captchaDoc.FailureCount >= config.MaxFailures)
                {
                    return;
                }

                var channel = await user.GetOrCreateDMChannelAsync();
                if (channel == null)
                {
                    return;
                }


                Stream imageStream = CaptchaGen.NetCore.ImageFactory.BuildImage(captchaDoc.Captcha, 100, 150, 25, 10, ImageFormatType.Jpeg);

                try
                {
                    await user.SendFileAsync(imageStream, "captcha.jpg", $"Please run the Verify command in order to speak in {user.Guild.Name}. ie. `lithium.moderation.Verify {user.Guild.Id} <code>`");
                }
                catch
                {
                    var guildChannel = user.Guild.GetTextChannel(config.ChannelId);
                    if (guildChannel != null)
                    {
                         await guildChannel.SendFileAsync(imageStream, "captcha.jpg", $"{user.Mention} Please run the Verify command in order to speak in {user.Guild.Name}. ie. `lithium.moderation.Verify {user.Guild.Id} <code>`");
                    }
                }
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
    }
}