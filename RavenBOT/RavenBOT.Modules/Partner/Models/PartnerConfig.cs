using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RavenBOT.Modules.Partner.Models
{
    public class PartnerConfig
    {
        public static string DocumentName(ulong guildId)
        {
            return $"PartnerConfig-{guildId}";
        }

        public PartnerConfig(ulong guildId)
        {
            GuildId = guildId;
        }

        public PartnerConfig() {}

        public ulong GuildId { get; set; }
        public ulong ReceiverChannelId { get; set; } = 0;

        public bool Enabled { get; set; } = false;

        public bool UseThumb { get; set; } = false;

        public string Message { get; set; } = null;

        public string ImageUrl { get; set; } = null;

        public int ServerCount { get; set; }
        public int UserCount { get; set; }

        public RGB Color { get; set; } = new RGB(0, 0, 0);

        public class RGB
        {
            public RGB(int r, int g, int b)
            {
                R = r;
                G = g;
                B = b;
            }

            public RGB() {}

            /// <summary>
            ///     Gets or sets the blue value
            /// </summary>
            public int B { get; set; }

            /// <summary>
            ///     Gets or sets the green value
            /// </summary>
            public int G { get; set; }

            /// <summary>
            ///     Gets or sets the red value
            /// </summary>
            public int R { get; set; }
        }

        public async Task<IInviteMetadata> GetInviteAsync(SocketGuild guild)
        {
            try
            {
                IInviteMetadata inviteData = null;
                if (guild.CurrentUser.GuildPermissions.ManageGuild)
                {
                    var currentInvites = await guild.GetInvitesAsync();
                    currentInvites = currentInvites.Where(x => x.MaxUses == 0 && x.MaxAge == 0 && !x.IsRevoked && !x.IsTemporary).ToList();
                    if (currentInvites.Any())
                    {
                        inviteData = currentInvites.First();
                    }

                    return inviteData;                    
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }

        }

        public async Task<(bool, EmbedBuilder)> GetEmbedAsync(SocketGuild guild)
        {
            var builder = new EmbedBuilder();
            builder.Description = Message ?? "";

            builder.Color = new Discord.Color(Color.R, Color.G, Color.B);

            builder.Footer = new EmbedFooterBuilder()
            {
                Text = $"Users: {guild.MemberCount}"
            };

            builder.Author = new EmbedAuthorBuilder()
            {
                Name = guild.Name,
                IconUrl = guild.IconUrl
            };

            var invite = await GetInviteAsync(guild);

            builder.Fields.Add(new EmbedFieldBuilder
            {
                Name = "Invite",
                Value = invite?.Url ?? "N/A This server does not have a non-expiring invite available or the bot does not have manage server permissions."
            });

            return (invite != null, builder);
        }
    }
}