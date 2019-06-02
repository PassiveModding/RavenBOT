using System.Globalization;
using System.Threading.Tasks;
using Discord.WebSocket;
using RavenBOT.Modules.Greetings.Models;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Greetings.Methods
{
    public class GreetingsService : IServiceable
    {
        private IDatabase Database {get;}
        private DiscordShardedClient Client {get;}
        public GreetingsService(IDatabase database, DiscordShardedClient client)
        {
            Database = database;
            Client = client;
            Client.UserJoined += UserJoined;
            Client.UserLeft += UserLeft;
        }

        public async Task UserJoined(SocketGuildUser user)
        {
            var config = GetWelcomeConfig(user.Guild.Id);
            if (!config.Enabled)
            {
                return;
            }

            var message = DoReplacements(config.WelcomeMessage ?? "Welcome to {servername} enjoy your stay!", user);

            if (config.DirectMessage)
            {
                var dmChannel = await user.GetOrCreateDMChannelAsync();
                if (dmChannel == null)
                {
                    return;
                }
                
                await dmChannel.SendMessageAsync(message);
            }
            else
            {
                var channel = user.Guild.GetTextChannel(config.WelcomeChannel);
                if (channel == null)
                {
                    return;
                }

                await channel.SendMessageAsync(message);
            }
        }

        public async Task UserLeft(SocketGuildUser user)
        {
            var config = GetGoodbyeConfig(user.Guild.Id);
            if (!config.Enabled)
            {
                return;
            }

            var message = DoReplacements(config.GoodbyeMessage ?? "Goodbye! Hope you enjoyed your time in {servername}", user);

            if (config.DirectMessage)
            {
                var dmChannel = await user.GetOrCreateDMChannelAsync();
                if (dmChannel == null)
                {
                    return;
                }
                
                await dmChannel.SendMessageAsync(message);
            }
            else
            {
                var channel = user.Guild.GetTextChannel(config.GoodbyeChannel);
                if (channel == null)
                {
                    return;
                }

                await channel.SendMessageAsync(message);
            }
        }

        public void SaveGoodbyeConfig(GoodbyeConfig config)
        {
            Database.Store(config, GoodbyeConfig.DocumentName(config.GuildId));
        }

        public void SaveWelcomeConfig(WelcomeConfig config)
        {
            Database.Store(config, WelcomeConfig.DocumentName(config.GuildId));
        }

        public GoodbyeConfig GetGoodbyeConfig(ulong guildId)
        {
            var config = Database.Load<GoodbyeConfig>(GoodbyeConfig.DocumentName(guildId));

            if (config == null)
            {
                config = new GoodbyeConfig(guildId);
                Database.Store(config, GoodbyeConfig.DocumentName(guildId));
            }

            return config;
        }
        public WelcomeConfig GetWelcomeConfig(ulong guildId)
        {
            var config = Database.Load<WelcomeConfig>(WelcomeConfig.DocumentName(guildId));

            if (config == null)
            {
                config = new WelcomeConfig(guildId);
                Database.Store(config, WelcomeConfig.DocumentName(guildId));
            }

            return config;
        }

        public string DoReplacements(string original, SocketGuildUser user)
        {
            original = original.Replace("{username}", user.Username, true, CultureInfo.InvariantCulture)
                                .Replace("{servername}", user.Guild.Name, true, CultureInfo.InvariantCulture)
                                .Replace("{nickname}", user.Nickname ?? user.Username, true, CultureInfo.InvariantCulture)
                                .Replace("{userid}", user.Id.ToString(), true, CultureInfo.InvariantCulture)
                                .Replace("{discriminator}", user.Discriminator, true, CultureInfo.InvariantCulture)
                                .Replace("{mention}", user.Mention, true, CultureInfo.InvariantCulture)
                                .Replace("{serverownerusername}", user.Guild.Owner.Username, true, CultureInfo.InvariantCulture)
                                .Replace("{serverownernickname}", user.Guild.Owner.Nickname ?? user.Guild.Owner.Username, true, CultureInfo.InvariantCulture)
                                .Replace("{serverid}", user.Guild.Id.ToString(), true, CultureInfo.InvariantCulture);
            return original;
        }


    }
}