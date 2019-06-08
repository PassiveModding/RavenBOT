using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Extensions;
using RavenBOT.Handlers;
using RavenBOT.Modules.Music.Methods;
using RavenBOT.Modules.Music.Preconditions;
using Victoria;
using Victoria.Entities;

namespace RavenBOT.Modules.Music.Modules
{
    [Group ("Music")]
    public class Audio : InteractiveBase<ShardedCommandContext>
    {
        private LavaPlayer player;
        private readonly LavaShardClient LavaShardClient;
        private readonly LavaRestClient RestClient;

        public VictoriaService Vic { get; }
        public LogHandler Logger { get; }

        public Audio (VictoriaService vic, LogHandler logger)
        {
            Vic = vic;
            Logger = logger;
            RestClient = vic.RestClient;
            LavaShardClient = vic.Client;
        }

        protected override void BeforeExecute (CommandInfo command)
        {
            player = LavaShardClient.GetPlayer (Context.Guild.Id);
            base.BeforeExecute (command);
        }

        [Command ("Join"), InAudioChannel]
        [Summary("Joins the audio channel you are currently in")]
        public async Task Join ()
        {
            await LavaShardClient.ConnectAsync (await Context.User.GetVoiceChannel (), Context.Channel as ITextChannel);
            await ReplyAsync ("Connected!");
        }

        [Command ("move"), InAudioChannel]
        [Summary("Moves the bot to a new audio channel")]
        public async Task MoveAsync ()
        {
            var old = player.VoiceChannel;
            await LavaShardClient.MoveChannelsAsync (await Context.User.GetVoiceChannel ());
            await ReplyAsync ($"Moved from {old.Name} to {player.VoiceChannel.Name}!");
        }

        [Command ("Play"), InAudioChannel]
        [Summary("Plays the specified track or adds it to the queue")]
        public async Task PlayAsync ([Remainder] string query)
        {
            var search = await RestClient.SearchYouTubeAsync(query);
            if (search.LoadType == LoadType.NoMatches ||
                search.LoadType == LoadType.LoadFailed)
            {
                await ReplyAsync ("Nothing found");
                return;
            }

            var track = search.Tracks.FirstOrDefault ();

            //If there is no player, join the current channel and set the player
            if (player == null)
            {
                await Join();
                player = LavaShardClient.GetPlayer(Context.Guild.Id);
            }

            if (player.IsPlaying)
            {
                player.Queue.Enqueue (track);
                await ReplyAsync ($"{track.Title} has been queued.");
            }
            else
            {
                await player.PlayAsync (track);
                await ReplyAsync ($"Now Playing: {track.Title}");
            }
        }

        [Command ("Disconnect"), InAudioChannel (true)]
        [Alias("Stop")]
        [Summary("Disconnects from the audio channel")]
        public async Task StopAsync ()
        {
            await LavaShardClient.DisconnectAsync (player.VoiceChannel);
            await ReplyAsync ("Disconnected!");
        }

        [Command ("Skip"), InAudioChannel (true)]
        [Summary("Skips the current song and plays the next in queue")]
        public async Task SkipAsync ()
        {
            try
            {
                var skipped = await player.SkipAsync ();
                await ReplyAsync ($"Skipped: {skipped.Title}\nNow Playing: {player.CurrentTrack.Title}");
            }
            catch
            {
                await ReplyAsync ("There are no more items left in queue.");
            }
        }

        [Command ("NowPlaying")]
        [Alias("np")]
        [Summary("Displays information about the song that is currently playing")]
        public async Task NowPlaying ()
        {
            if (player.CurrentTrack is null)
            {
                await ReplyAsync ("There is no track playing right now.");
                return;
            }

            var track = player.CurrentTrack;
            var thumb = await track.FetchThumbnailAsync ();
            var embed = new EmbedBuilder ()
                .WithAuthor ($"Now Playing {player.CurrentTrack.Title}", thumb, $"{track.Uri}")
                .WithThumbnailUrl (thumb)
                .AddField ("Author", track.Author, true)
                .AddField ("Length", track.Length, true)
                .AddField ("Position", track.Position, true)
                .AddField ("Streaming?", track.IsStream, true);

            await ReplyAsync ("", false, embed.Build ());
        }

        [Command ("Lyrics")]
        [Summary("Attempts to fetch lyrics for the current song")]
        public async Task LyricsAsync ()
        {
            if (player.CurrentTrack is null)
            {
                await ReplyAsync ("There is no track playing right now.");
                return;
            }

            var lyrics = await player.CurrentTrack.FetchLyricsAsync ();
            var thumb = await player.CurrentTrack.FetchThumbnailAsync ();

            var embed = new EmbedBuilder ()
                .WithImageUrl (thumb)
                .WithDescription (lyrics)
                .WithAuthor ($"Lyrics For {player.CurrentTrack.Title}", thumb);

            await ReplyAsync ("", false, embed.Build ());
        }

        [Command ("Queue")]
        [Summary("Displays all tracks  in the queue")]
        public Task Queue ()
        {
            var tracks = player.Queue.Items.Cast<LavaTrack> ().Select (x => x.Title);
            return ReplyAsync (tracks.Count () is 0 ?
                "No tracks in queue." : string.Join ("\n", tracks));
        }

        [Command("Configure")]
        [Summary("Reconfigured Victoria Sharded Client and Rest client based on the config file")]
        [RequireOwner]
        public Task Configure()
        {
            return Vic.Configure(Context.Client.GetShardFor(Context.Guild));
        }

        [Command("Stats")]
        [Summary("Lavalink server stats")]
        [RequireOwner]
        public async Task Stats()
        {
            var stats = LavaShardClient.ServerStats;

            await ReplyAsync($"CPU Cores: {stats.Cpu.Cores}\n" +
                            $"CPU Lavalink Load: {stats.Cpu.LavalinkLoad}\n" +
                            $"CPU System Load: {stats.Cpu.SystemLoad}\n" +
                            $"Average Frames Deficit: {stats.Frames.Deficit}\n" +
                            $"Average Frames Nulled: {stats.Frames.Nulled}\n" +
                            $"Average Frames Sent: {stats.Frames.Sent}\n" +
                            $"Memory Allocated: {stats.Memory.Allocated}\n" +
                            $"Memory Free: {stats.Memory.Free}\n" +
                            $"Memory Reservable: {stats.Memory.Reservable}\n" +
                            $"Memory Used: {stats.Memory.Used}\n" +
                            $"Player Count: {stats.PlayerCount}\n" +
                            $"Playing Players: {stats.PlayingPlayers}\n" +
                            $"Uptime: {stats.Uptime.GetReadableLength()}");
        }
    }
}