using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RavenBOT.Discord.Context;
using RavenBOT.Discord.Preconditions;
using RavenBOT.Handlers;
using RavenBOT.Models;

namespace RavenBOT.Modules
{
    //Base is what we inherit our context from, ie ReplyAsync, Context.Guild etc.
    public class Example : Base
    {
        //The Main Command Name
        [Command("ServerStats")]
        //A summary of what the command does
        [Summary("Bot Statistics Command")]
        //Extra notes on the command
        [Remarks("Can only be run within a server")]
        //A Precondition, limiting access to the command
        [RequireContext(ContextType.Guild)]
        public async Task Stats()
        {
            var embed = new EmbedBuilder
            {
                Color = Color.Blue
            };
            embed.AddField("Server Name", Context.Guild.Name);
            embed.AddField("Server Owner", $"Name: {Context.Guild.Owner}\n" +
                                           $"ID: {Context.Guild.OwnerId}");
            embed.AddField("Users", $"User Count: {Context.Guild.MemberCount}\n" +
                                    $"Cached User Count: {Context.Guild.Users.Count}\n" +
                                    $"Cached Bots Count: {Context.Guild.Users.Count(x => x.IsBot)}");
            embed.AddField("Counts", $"Channels: {Context.Guild.TextChannels.Count + Context.Guild.VoiceChannels.Count}\n" +
                                     $"Text Channels: {Context.Guild.TextChannels.Count}\n" +
                                     $"Voice Channels: {Context.Guild.VoiceChannels.Count}\n" +
                                     $"Categories: {Context.Guild.CategoryChannels.Count}");
            await ReplyAsync("", false, embed.Build());
        }

        [Command("Say")]
        [Alias("Echo", "Repeat")]
        [Summary("Repeats the given message")]
        public async Task Echo([Remainder] string message)
        {
            await ReplyAsync(message);
        }

        [Command("GetDatabaseGuildID")]
        [RequireContext(ContextType.Guild)]
        [Summary("Loads the guildID from the database")]
        public async Task DBLoad()
        {
            await SimpleEmbedAsync(Context.Server.ID.ToString());
        }

        [Command("DownloadConfig")]
        [RequireContext(ContextType.Guild)]
        [GuildOwner]
        [Summary("Downloads the config file of the guild")]
        public async Task DBDownload()
        {
            var DC = Context.Server;
            var serialised = JsonConvert.SerializeObject(DC, Formatting.Indented);

            var uniEncoding = new UnicodeEncoding();
            using (Stream ms = new MemoryStream())
            {
                var sw = new StreamWriter(ms, uniEncoding);
                try
                {
                    sw.Write(serialised);
                    sw.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    //You can send files from a stream in discord too, This allows us to avoid having to read and write directly from a file for this command.
                    await Context.Channel.SendFileAsync(ms, $"{Context.Guild.Name}[{Context.Guild.Id}] BotConfig.json");
                }
                finally
                {
                    sw.Dispose();
                }
            }
        }

        [Command("CustomPrefix")]
        [RequireContext(ContextType.Guild)]
        [GuildOwner]
        [Summary("Loads the guildID from the database")]
        [Remarks("This command can only be invoked by the server owner")]
        public async Task CustomPrefix([Remainder] string prefix = null)
        {
            //Modify the prefix and then update the object within the database.
            Context.Server.Settings.CustomPrefix = prefix;
            Context.Server.Save();

            //If prefix is null, we default back to the default bot prefix
            await SimpleEmbedAsync($"Prefix is now: {prefix ?? Context.Provider.GetRequiredService<ConfigModel>().Prefix}");
        }

        [Command("embedreaction")]
        [Summary("Sends a custom message that performs a specific action upon reacting")]
        [Remarks("N/A")]
        public async Task Test_EmedReactionReply(bool expires, bool singleuse, bool singleuser)
        {
            var one = new Emoji("1⃣");
            var two = new Emoji("2⃣");

            var embed = new EmbedBuilder()
                .WithTitle("Choose one")
                .AddField(one.Name, "Beer", true)
                .AddField(two.Name, "Drink", true)
                .Build();

            //This message does not expire after a single
            //it will not allow a user to react more than once
            //it allows more than one user to react
            await InlineReactionReplyAsync(new ReactionCallbackData("text", embed, expires, singleuse)
                    .WithCallback(one, (c, r) =>
                    {
                        //You can do additional things with your reaction here, NOTE: c references this commands context whereas r references our added reaction.
                        //This is important to note because context.user can be a different user to reaction.user
                        return c.Channel.SendMessageAsync($"{r.User.Value.Mention} Here you go :beer:");
                    })
                    .WithCallback(two, (c, r) => c.Channel.SendMessageAsync($"{r.User.Value.Mention} Here you go :tropical_drink:")), singleuser
            );
        }

        [Command("SetShards")]
        [RequireContext(ContextType.Guild)]
        [RequireOwner]
        [Summary("Set total amount of shards for the bot")]
        public async Task SetShards(int shards)
        {
            //Here we can access the service provider via our custom context.
            var config = Context.Provider.GetRequiredService<ConfigModel>();
            config.shards = shards;
            Context.Provider.GetRequiredService<DatabaseHandler>().Execute<ConfigModel>(DatabaseHandler.Operation.SAVE, config, "Config");
            await SimpleEmbedAsync($"Shard Count updated to: {shards}\n" +
                                   $"This will be effective after a restart.\n" +
                                   //Note, 2500 Guilds is the max amount per shard, so this should be updated based on around 2000 as if you hit the 2500 limit discord will ban the account associated.
                                   $"Recommended shard count: {(Context.Client.Guilds.Count / 2000 < 1 ? 1 : Context.Client.Guilds.Count / 2000)}");
        }
    }
}