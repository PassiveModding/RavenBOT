using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using RavenBOT.Discord.Context;
using RavenBOT.Discord.Preconditions;
using RavenBOT.Handlers;

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
            embed.AddField("Server Owner", $"Name: {Context.Socket.Guild.Owner}\n" +
                                           $"ID: {Context.Socket.Guild.OwnerId}");
            embed.AddField("Users", $"User Count: {Context.Socket.Guild.MemberCount}\n" +
                                    $"Cached User Count: {Context.Socket.Guild.Users.Count}\n" +
                                    $"Cached Bots Count: {Context.Socket.Guild.Users.Count(x => x.IsBot)}");
            embed.AddField("Counts", $"Channels: {Context.Socket.Guild.TextChannels.Count + Context.Socket.Guild.VoiceChannels.Count}\n" +
                                     $"Text Channels: {Context.Socket.Guild.TextChannels.Count}\n" +
                                     $"Voice Channels: {Context.Socket.Guild.VoiceChannels.Count}\n" +
                                     $"Categories: {Context.Socket.Guild.CategoryChannels.Count}");
            await ReplyAsync("", false, embed.Build());
        }

        [Command("Say")]
        [Alias("Echo")]
        [Summary("Repeats the given message")]
        public async Task Echo([Remainder]string message)
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
            var DC = DatabaseHandler.GetGuild(Context.Guild.Id);
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
        public async Task CustomPrefix([Remainder]string prefix = null)
        {
            //Modify the prefix and then update the object within the database.
            Context.Server.Settings.CustomPrefix = prefix;
            Context.Server.Save();

            //If prefix is null, we default back to the default bot prefix
            await SimpleEmbedAsync($"Prefix is now: {prefix ?? Context.Prefix}");
        }
    }
}
