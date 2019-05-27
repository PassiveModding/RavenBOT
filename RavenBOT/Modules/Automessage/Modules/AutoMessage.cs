using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Modules.Automessage.Methods;
using RavenBOT.Modules.Automessage.Models;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Automessage.Modules
{
    [Group("Automessage.")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(Discord.GuildPermission.Administrator)]
    public class AutoMessage : InteractiveBase<ShardedCommandContext>
    {
        public AutoMessage(IDatabase database, DiscordShardedClient client)
        {
            AutomessageHandler = new AutomessageHandler(database, client);
        }

        public AutomessageHandler AutomessageHandler { get; }

        [Command("AddChannel")]
        public async Task AddAutoMessageChannelAsync(int messageCount, [Remainder]string message)
        {
            var config = AutomessageHandler.GetAutomessageChannel(Context.Channel.Id);

            if (config != null)
            {
                await ReplyAsync("Channel Already Exists.");
                return;
            }

            var newChannel = new AutomessageChannel(Context.Channel.Id)
            {
                Response = message,
                RespondOn = messageCount
            };

            AutomessageHandler.SaveAutomessageChannel(newChannel);
            await ReplyAsync("Channel Added.");
        }

        [Command("RemoveChannel")]
        public async Task RemoveAutoMessageChannelAsync()
        {
            if (AutomessageHandler.Cache.TryGetValue(Context.Channel.Id, out var config))
            {
                if (AutomessageHandler.RemoveAutomessageChannel(config.Value))
                {
                    await ReplyAsync("Channel Removed.");
                }
                else
                {
                    await ReplyAsync("Error removing channel, try again.");
                }
            }
            else
            {
                var dbConfig = AutomessageHandler.GetAutomessageChannel(Context.Channel.Id);
                if (dbConfig == null)
                {
                    await ReplyAsync("Done.");
                    return;
                }

                if (AutomessageHandler.RemoveAutomessageChannel(dbConfig))
                {
                    await ReplyAsync("Channel Removed.");
                }
                else
                {
                    await ReplyAsync("Error removing channel, try again.");
                }
            }
        }
    }
}