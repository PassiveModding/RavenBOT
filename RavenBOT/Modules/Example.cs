namespace RavenBOT.Modules
{
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using global::Discord;

    using global::Discord.Addons.Interactive;

    using global::Discord.Commands;

    using Microsoft.Extensions.DependencyInjection;

    using Newtonsoft.Json;

    using RavenBOT.Discord.Context;
    using RavenBOT.Discord.Preconditions;
    using RavenBOT.Handlers;
    using RavenBOT.Models;

    /// <summary>
    /// Base is what we inherit our context from, ie ReplyAsync, Context.Guild etc.
    /// Example is our module name
    /// </summary>
    [Group("Example")]// You can add a group attribute to a module to prefix all commands in that module. ie. +Example ServerStats rather than +ServerStats
    [RequireContext(ContextType.Guild)] // You can also use precondition attributes on a module to ensure commands are only run if they pass the precondition
    public class Example : Base
    {
        /// <summary>
        /// The stats.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Command("ServerStats")] // The Main Command Name
        [Summary("Bot Statistics Command")] // A summary of what the command does
        [Remarks("Can only be run within a server")] // Extra notes on the command
        [RequireContext(ContextType.Guild)] // A Precondition, limiting access to the command
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
            await ReplyAsync(string.Empty, false, embed.Build());
        }

        /// <summary>
        /// Echos the provided message
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Command("Say")]
        [Alias("Echo", "Repeat")]// Commands can be initialized using different names, ie, Echo, Repeat and Say will all have the same effect.
        [Summary("Repeats the given message")]
        public async Task Echo([Remainder] string message)
        {
            await ReplyAsync(message);
        }

        /// <summary>
        /// The db load.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Command("GetDatabaseGuildID")]
        [RequireContext(ContextType.Guild)]
        [Summary("Loads the guildID from the database")]
        public async Task DBLoad()
        {
            // SimpleEmbedAsync is one of our additions in our custom context
            // This is an example of how you can use custom additions to the bot context. ie. Context.Server
            await SimpleEmbedAsync(Context.Server.ID.ToString());
        }

        /// <summary>
        /// Downloads the stored json config of the guild
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Command("DownloadConfig")]
        [RequireContext(ContextType.Guild)]
        [GuildOwner]
        [Summary("Downloads the config file of the guild")]
        public async Task DBDownload()
        {
            var database = Context.Server;
            var serialized = JsonConvert.SerializeObject(database, Formatting.Indented);

            var uniEncoding = new UnicodeEncoding();
            using (Stream ms = new MemoryStream())
            {
                var sw = new StreamWriter(ms, uniEncoding);
                try
                {
                    sw.Write(serialized);
                    sw.Flush();
                    ms.Seek(0, SeekOrigin.Begin);

                    // You can send files from a stream in discord too, This allows us to avoid having to read and write directly from a file for this command.
                    await Context.Channel.SendFileAsync(ms, $"{Context.Guild.Name}[{Context.Guild.Id}] BotConfig.json");
                }
                finally
                {
                    sw.Dispose();
                }
            }
        }

        /// <summary>
        /// Sets a custom prefix for the bot in the current server
        /// </summary>
        /// <param name="prefix">
        /// The prefix.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Command("CustomPrefix")]
        [RequireContext(ContextType.Guild)]
        [GuildOwner]
        [Summary("Loads the guildID from the database")]
        [Remarks("This command can only be invoked by the server owner")]
        public async Task CustomPrefix([Remainder] string prefix = null)
        {
            // Modify the prefix and then update the object within the database.
            Context.Server.Settings.CustomPrefix = prefix;
            Context.Server.Save();

            // If prefix is null, we default back to the default bot prefix
            await SimpleEmbedAsync($"Prefix is now: {prefix ?? Context.Provider.GetRequiredService<ConfigModel>().Prefix}");
        }

        /// <summary>
        /// Sends a custom message that performs a specific action upon reacting
        /// </summary>
        /// <param name="expires">True = Expires after first use</param>
        /// <param name="singleuse">True = Only one use per user</param>
        /// <param name="singleuser">True = Only the command invoker can use</param>
        /// <returns>Something or something</returns>
        [Command("embedreaction")]
        [Summary("Sends a custom message that performs a specific action upon reacting")]
        [Remarks("N/A")]
        public async Task Test_EmbedReactionReply(bool expires, bool singleuse, bool singleuser)
        {
            var one = new Emoji("1⃣");
            var two = new Emoji("2⃣");

            var embed = new EmbedBuilder()
                .WithTitle("Choose one")
                .AddField(one.Name, "Beer", true)
                .AddField(two.Name, "Drink", true)
                .Build();

            // This message does not expire after a single
            // it will not allow a user to react more than once
            // it allows more than one user to react
            await InlineReactionReplyAsync(new ReactionCallbackData("text", embed, expires, singleuse)
                    .WithCallback(one, (c, r) =>
                    {
                        // You can do additional things with your reaction here, NOTE: c references this commands context whereas r references our added reaction.
                        // This is important to note because context.user can be a different user to reaction.user
                        var reactor = r.User.Value;
                        return c.Channel.SendMessageAsync($"{reactor.Mention} Here you go :beer:");
                    }).WithCallback(two, (c, r) => c.Channel.SendMessageAsync($"{r.User.Value.Mention} Here you go :tropical_drink:")),
                singleuser);
        }

        /// <summary>
        /// Set the total amount of shards for the bot
        /// </summary>
        /// <param name="shards">
        /// The shard count
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Command("SetShards")]
        [RequireContext(ContextType.Guild)]
        [RequireOwner]
        [Summary("Set total amount of shards for the bot")]
        public async Task SetShards(int shards)
        {
            // Here we can access the service provider via our custom context.
            var config = Context.Provider.GetRequiredService<ConfigModel>();
            config.Shards = shards;
            Context.Provider.GetRequiredService<DatabaseHandler>().Execute<ConfigModel>(DatabaseHandler.Operation.SAVE, config, "Config");
            await SimpleEmbedAsync($"Shard Count updated to: {shards}\n" +
                                   "This will be effective after a restart.\n" +

                                   // Note, 2500 Guilds is the max amount per shard, so this should be updated based on around 2000 as if you hit the 2500 limit discord will ban the account associated.
                                   $"Recommended shard count: {(Context.Client.Guilds.Count / 2000 < 1 ? 1 : Context.Client.Guilds.Count / 2000)}");
        }

        /// <summary>
        /// toggle message logging.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Command("ToggleMessageLog")]
        [RequireContext(ContextType.Guild)]
        [RequireOwner]
        [Summary("Toggle the logging of all user messages to console")]
        public async Task ToggleMessageLog()
        {
            var config = Context.Provider.GetRequiredService<ConfigModel>();
            config.LogUserMessages = !config.LogUserMessages;
            Context.Provider.GetRequiredService<DatabaseHandler>().Execute<ConfigModel>(DatabaseHandler.Operation.SAVE, config, "Config");
            await SimpleEmbedAsync($"Log User Messages: {config.LogUserMessages}");
        }

        /// <summary>
        /// toggle command logging.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Command("ToggleCommandLog")]
        [RequireContext(ContextType.Guild)]
        [RequireOwner]
        [Summary("Toggle the logging of all user messages to console")]
        public async Task ToggleCommandLog()
        {
            var config = Context.Provider.GetRequiredService<ConfigModel>();
            config.LogCommandUsages = !config.LogCommandUsages;
            Context.Provider.GetRequiredService<DatabaseHandler>().Execute<ConfigModel>(DatabaseHandler.Operation.SAVE, config, "Config");
            await SimpleEmbedAsync($"Log Command Usages: {config.LogCommandUsages}");
        }
    }
}