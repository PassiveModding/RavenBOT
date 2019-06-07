using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Extensions;
using RavenBOT.Modules.Music.Methods;
using RavenBOT.Modules.Music.Preconditions;
using Victoria;
using Victoria.Entities;

namespace RavenBOT.Modules.Music.Modules
{
    [Group("Music")]
    public class Audio : InteractiveBase<ShardedCommandContext>
    {
        private LavaPlayer player;
        private readonly LavaShardClient LavaShardClient;
        private readonly LavaRestClient RestClient;

        public Audio (VictoriaService vic)
        {
            RestClient = vic.RestClient;
            LavaShardClient = vic.Client;
        }

        protected override void BeforeExecute (CommandInfo command)
        {
            player = LavaShardClient.GetPlayer (Context.Guild.Id);
            base.BeforeExecute (command);
        }

        [Command ("Join"), InAudioChannel]
        public async Task Join ()
        {
            await LavaShardClient.ConnectAsync (await Context.User.GetVoiceChannel (), Context.Channel as ITextChannel);
            await ReplyAsync ("Connected!");
        }

        [Command ("move"), InAudioChannel]
        public async Task MoveAsync ()
        {
            var old = player.VoiceChannel;
            await LavaShardClient.MoveChannelsAsync (await Context.User.GetVoiceChannel ());
            await ReplyAsync ($"Moved from {old.Name} to {player.VoiceChannel.Name}!");
        }

        [Command ("Play"), InAudioChannel (true)]
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
        public async Task StopAsync ()
        {
            await LavaShardClient.DisconnectAsync (player.VoiceChannel);
            await ReplyAsync ("Disconnected!");
        }

        [Command ("Skip"), InAudioChannel (true)]
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
        public Task Queue ()
        {
            var tracks = player.Queue.Items.Cast<LavaTrack> ().Select (x => x.Title);
            return ReplyAsync (tracks.Count () is 0 ?
                "No tracks in queue." : string.Join ("\n", tracks));
        }
    }
}