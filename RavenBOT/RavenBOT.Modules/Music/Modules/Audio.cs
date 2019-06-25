using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Newtonsoft.Json;
using RavenBOT.Common.Attributes;
using RavenBOT.Common.Handlers;
using RavenBOT.Common.Services;
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

        public VictoriaService Vic { get; }
        public LogHandler Logger { get; }
        public HelpService HelpService { get; }

        public Audio(VictoriaService vic, LogHandler logger, HelpService helpService)
        {
            Vic = vic;
            Logger = logger;
            HelpService = helpService;
            RestClient = vic.RestClient;
            LavaShardClient = vic.Client;
        }

        protected override void BeforeExecute(CommandInfo command)
        {
            player = LavaShardClient.GetPlayer(Context.Guild.Id);
            base.BeforeExecute(command);
        }

        [Command("Help")]
        public async Task HelpAsync()
        {
            var res = await HelpService.PagedHelpAsync(Context, true, new List<string>
            {
                "music"
            }, "This module handles music commands and setup.");

            if (res != null)
            {
                await PagedReplyAsync(res, new ReactionList
                {
                    Backward = true,
                        First = false,
                        Forward = true,
                        Info = false,
                        Jump = true,
                        Last = false,
                        Trash = true
                });
            }
            else
            {
                await ReplyAsync("N/A");
            }
        }

        [Command("Join"), InAudioChannel]
        [Summary("Joins the audio channel you are currently in")]
        public async Task Join()
        {
            await LavaShardClient.ConnectAsync(await Context.User.GetVoiceChannel(), Context.Channel as ITextChannel);
            await ReplyAsync("Connected!");
        }

        [Command("move"), InAudioChannel]
        [Summary("Moves the bot to a new audio channel")]
        public async Task MoveAsync()
        {
            var old = player.VoiceChannel;
            await LavaShardClient.MoveChannelsAsync(await Context.User.GetVoiceChannel());
            await ReplyAsync($"Moved from {old.Name} to {player.VoiceChannel.Name}!");
        }

        [Command("Play"), InAudioChannel]
        [Summary("Plays the specified track or adds it to the queue")]
        public async Task PlayAsync([Remainder] string query)
        {
            var regExp = new Regex(@"youtu(.*)(be|com).*(list=([a-zA-Z0-9_\-]+))");
            var match = regExp.Match(query);
            Victoria.Entities.SearchResult search;
            if (match.Success)
            {
                search = await RestClient.SearchTracksAsync(query, false);
            }
            else
            {
                search = await RestClient.SearchYouTubeAsync(query);
            }

            if (search.LoadType == LoadType.NoMatches ||
                search.LoadType == LoadType.LoadFailed)
            {
                await ReplyAsync("Nothing found");
                return;
            }

            //If there is no player, join the current channel and set the player
            if (player == null)
            {
                await Join();
                player = LavaShardClient.GetPlayer(Context.Guild.Id);
            }

            var track = search.Tracks.FirstOrDefault();

            if (player.IsPlaying)
            {
                if (search.LoadType == LoadType.PlaylistLoaded)
                {
                    foreach (var playlistTrack in search.Tracks)
                    {
                        player.Queue.Enqueue(playlistTrack);
                    }
                    await ReplyAsync($"{search.Tracks.Count()} tracks added from playlist: {search.PlaylistInfo.Name}");
                }
                else
                {
                    player.Queue.Enqueue(track);
                    await ReplyAsync($"{track.Title} has been queued.");
                }
            }
            else
            {
                await player.PlayAsync(track);
                await ReplyAsync($"Now Playing: {track.Title}");
                if (search.LoadType == LoadType.PlaylistLoaded)
                {
                    foreach (var playlistTrack in search.Tracks.Where(x => x.Id != track.Id))
                    {
                        player.Queue.Enqueue(playlistTrack);
                    }
                    await ReplyAsync($"{search.Tracks.Count()} tracks added from playlist: {search.PlaylistInfo.Name}");
                }
            }
        }

        [Command("Disconnect"), InAudioChannel(true)]
        [Alias("Stop")]
        [Summary("Disconnects from the audio channel")]
        public async Task StopAsync()
        {
            await LavaShardClient.DisconnectAsync(player.VoiceChannel);
            await ReplyAsync("Disconnected!");
        }

        [Command("Skip"), InAudioChannel(true)]
        [Summary("Skips the current song and plays the next in queue")]
        public async Task SkipAsync(int amount = 1)
        {
            try
            {
                if (amount <= 1)
                {
                    var skipped = await player.SkipAsync();
                    await ReplyAsync($"Skipped: {skipped.Title}\nNow Playing: {player.CurrentTrack.Title}");
                }
                else
                {
                    LavaTrack track = null;
                    for (int i = 0; i < amount; i++)
                    {
                        if (player.Queue.TryDequeue(out var trk))
                        {
                            if (trk is LavaTrack lavaTrack)
                            {
                                track = lavaTrack;
                            }
                        }
                    }
                    if (track != null)
                    {
                        await player.PlayAsync(track);
                        await ReplyAsync($"Skipped {amount} tracks\nNow Playing: {player.CurrentTrack.Title}");
                    }
                }
            }
            catch
            {
                await ReplyAsync("There are no more items left in queue.");
            }
        }

        [Command("NowPlaying")]
        [Alias("np")]
        [Summary("Displays information about the song that is currently playing")]
        public async Task NowPlaying()
        {
            if (player.CurrentTrack is null)
            {
                await ReplyAsync("There is no track playing right now.");
                return;
            }

            var track = player.CurrentTrack;
            var thumb = await track.FetchThumbnailAsync();
            var embed = new EmbedBuilder()
                .WithAuthor($"Now Playing {player.CurrentTrack.Title}", thumb, $"{track.Uri}")
                .WithThumbnailUrl(thumb)
                .AddField("Author", track.Author, true)
                .AddField("Length", track.Length, true)
                .AddField("Position", track.Position, true)
                .AddField("Streaming?", track.IsStream, true);

            await ReplyAsync("", false, embed.Build());
        }

        [Command("Lyrics")]
        [Summary("Attempts to fetch lyrics for the current song")]
        public async Task LyricsAsync()
        {
            if (player.CurrentTrack is null)
            {
                await ReplyAsync("There is no track playing right now.");
                return;
            }

            var lyrics = await Vic.ScrapeGeniusLyricsAsync(player.CurrentTrack.Title);

            if (lyrics == null)
            {
                await ReplyAsync("Could not fetch lyrics.");
                return;
            }

            var thumb = await player.CurrentTrack.FetchThumbnailAsync();

            var pager = new PaginatedMessage();
            var pages = new List<PaginatedMessage.Page>();
            string[] paragraphs = Regex.Split(lyrics, "(\n){2,}");
            foreach (var group in paragraphs)
            {
                //Ensure that we are not including un-necessary empty pages
                if (string.IsNullOrWhiteSpace(group))
                {
                    continue;
                }
                //Ensure that the length of the paragraph does not exceed the max embed length
                if (group.Length >= 2048)
                {
                    //Split the paragraph into sub-groups and add individually
                    var words = group.Split(" ");
                    var sb = new StringBuilder();
                    //Add the words of each group individually to ensure that the response is still coherent
                    foreach (var word in words)
                    {
                        if (sb.Length + word.Length >= 2048)
                        {
                            pages.Add(new PaginatedMessage.Page()
                            {
                                Description = sb.ToString()
                            });
                            sb.Clear();
                        }
                        sb.Append($"{word} ");
                    }

                    //Ensure that any remaining content is added to the pager.
                    var remaining = sb.ToString();
                    if (!string.IsNullOrWhiteSpace(remaining))
                    {
                        pages.Add(new PaginatedMessage.Page()
                        {
                            Description = remaining
                        });
                    }
                }
                else
                {
                    pages.Add(new PaginatedMessage.Page()
                    {
                        Description = group
                    });
                }
            }
            pager.Pages = pages;
            if (pager.Pages.Any())
            {
                pager.Pages.First().ImageUrl = thumb;
                pager.Pages.First().Author = new EmbedAuthorBuilder()
                {
                    Name = $"Lyrics For {player.CurrentTrack.Title}",
                    IconUrl = thumb
                };

                await PagedReplyAsync(pager, new ReactionList()
                {
                    Forward = true,
                        Backward = true,
                        Trash = true
                });
            }
        }

        [Command("Queue")]
        [Summary("Displays all tracks  in the queue")]
        public Task Queue()
        {
            var tracks = player.Queue.Items.Cast<LavaTrack>().Select(x => x.Title).ToList();

            int i = 0;
            var trackList = new List<string>();
            foreach (var track in tracks)
            {
                i++;
                trackList.Add($"{i} - {track}");
            }

            var response = trackList.Count == 0 ? "No tracks in queue." : string.Join("\n", trackList);
            return ReplyAsync(response.FixLength(2047));
        }

        [Command("Configure")]
        [Summary("Reconfigured Victoria Sharded Client and Rest client based on the config file")]
        [RavenRequireOwner]
        public Task Configure()
        {
            return Vic.Configure();
        }

        [Command("Setup")]
        [Summary("Creates a new victoria config for music.")]
        [RavenRequireOwner]
        public async Task SetupMusicAsync(string host, int port, string password)
        {
            var config = new VictoriaService.VictoriaConfig();
            config.MainConfig.Host = host;
            config.RestConfig.Host = config.MainConfig.Host;

            config.MainConfig.Port = port;
            config.RestConfig.Port = config.MainConfig.Port;

            config.MainConfig.Password = password;
            config.RestConfig.Password = config.MainConfig.Password;

            File.WriteAllText(Vic.ConfigPath, JsonConvert.SerializeObject(config, Formatting.Indented));
            await ReplyAsync("Victoria config created. Run the Configure command to setup music.");
        }

        [Command("SetAuthorization")]
        [Summary("Sets the authorization header for genius lyrics")]
        [RavenRequireOwner]
        public Task SetGeniusAuth([Remainder] string auth)
        {
            var doc = Vic.Database.Load<VictoriaService.GeniusConfig>(VictoriaService.GeniusConfig.DocumentName());
            if (doc == null)
            {
                doc = new VictoriaService.GeniusConfig();
            }
            doc.Authorization = auth;
            Vic.Database.Store(doc, VictoriaService.GeniusConfig.DocumentName());
            return ReplyAsync("Set.");
        }

        [Command("SetVolume")]
        [Alias("Set Volume")]
        [Summary("Sets the audio player's volume")]
        [InAudioChannel(true)]
        public async Task SetVolume(int volume = 100)
        {
            if (volume <= 0 || volume > 1000)
            {
                await ReplyAsync("Volume must be between 1 and 1000");
                return;
            }
            await player.SetVolumeAsync(volume);
            await ReplyAsync($"Volume set to {volume}");
        }

        [Command("Pause")]
        [Summary("Pauses the audio player if playing")]
        [InAudioChannel(true)]
        public async Task Pause()
        {
            await player.PauseAsync();
            await ReplyAsync("Paused.");
        }

        [Command("Resume")]
        [Summary("Resumed the audio player if paused")]
        [InAudioChannel(true)]
        public async Task Resume()
        {
            await player.ResumeAsync();
            await ReplyAsync("Resumed.");
        }

        [Command("ShowVolume")]
        [Alias("Volume")]
        [Summary("Displays the audio player's volume")]
        [InAudioChannel(true)]
        public async Task ShowVolume()
        {
            await ReplyAsync($"Volume is {player.CurrentVolume}");
        }

        [Command("Stats")]
        [Summary("Lavalink server stats")]
        [RavenRequireOwner]
        public async Task Stats()
        {
            var stats = LavaShardClient.ServerStats;

            await ReplyAsync($"CPU Cores: {stats?.Cpu?.Cores}\n" +
                $"CPU Lavalink Load: {stats?.Cpu?.LavalinkLoad}\n" +
                $"CPU System Load: {stats?.Cpu?.SystemLoad}\n" +
                $"Average Frames Deficit: {stats?.Frames?.Deficit}\n" +
                $"Average Frames Nulled: {stats?.Frames?.Nulled}\n" +
                $"Average Frames Sent: {stats?.Frames?.Sent}\n" +
                $"Memory Allocated: {stats?.Memory?.Allocated}\n" +
                $"Memory Free: {stats?.Memory?.Free}\n" +
                $"Memory Reservable: {stats?.Memory?.Reservable}\n" +
                $"Memory Used: {stats?.Memory?.Used}\n" +
                $"Player Count: {stats?.PlayerCount}\n" +
                $"Playing Players: {stats?.PlayingPlayers}\n" +
                $"Uptime: {stats?.Uptime.GetReadableLength()}");
        }
    }
}