using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace RavenBOT.Extensions
{
    public static class GuildExtensions
    {
        public static async Task<IVoiceChannel> GetVoiceChannel(this IUser user)
        {
            if (user is IGuildUser gUser)
            {
                var channels = await gUser.Guild.GetVoiceChannelsAsync();
                foreach (var channel in channels)
                {
                    var users = await channel.GetUsersAsync().FlattenAsync();
                    if (users.Any(x => x.Id == user.Id))
                    {
                        return channel;
                    }
                }
            }

            return null;
        }

        public static async Task<IUserMessage> EnsureMention(IMessageChannel channel, IEnumerable<IRole> roles, string messageContent, EmbedBuilder embed = null)
        {
            var unmentionable = roles.Where(x => !x.IsMentionable);
            foreach (var role in unmentionable)
            {
                await role.ModifyAsync(x => x.Mentionable = true);
            }

            var message = await channel.SendMessageAsync(messageContent, false, embed.Build());

            foreach (var role in unmentionable)
            {
                await role.ModifyAsync(x => x.Mentionable = false);
            }

            return message;
        }
        
        public static int DamerauLavenshteinDistance(this string s, string t)
        {
            var bounds = new { Height = s.Length + 1, Width = t.Length + 1 };

            int[, ] matrix = new int[bounds.Height, bounds.Width];

            for (int height = 0; height < bounds.Height; height++) { matrix[height, 0] = height; };
            for (int width = 0; width < bounds.Width; width++) { matrix[0, width] = width; };

            for (int height = 1; height < bounds.Height; height++)
            {
                for (int width = 1; width < bounds.Width; width++)
                {
                    int cost = (s[height - 1] == t[width - 1]) ? 0 : 1;
                    int insertion = matrix[height, width - 1] + 1;
                    int deletion = matrix[height - 1, width] + 1;
                    int substitution = matrix[height - 1, width - 1] + cost;

                    int distance = Math.Min(insertion, Math.Min(deletion, substitution));

                    if (height > 1 && width > 1 && s[height - 1] == t[width - 2] && s[height - 2] == t[width - 1])
                    {
                        distance = Math.Min(distance, matrix[height - 2, width - 2] + cost);
                    }

                    matrix[height, width] = distance;
                }
            }

            return matrix[bounds.Height - 1, bounds.Width - 1];
        }

        public static bool IsAdminOrGuildOwner(this IGuildUser user)
        {
            if (user?.Guild == null)
            {
                return false;
            }

            if (user.Guild.OwnerId == user.Id)
            {
                //Guild owner overrides all permissions
                return true;
            }

            return user.GuildPermissions.Administrator;
        }

        public static string GetMentionList(this IGuild guild, IEnumerable<ulong> roleIds)
        {
            var builder = new StringBuilder();
            foreach (var id in roleIds)
            {
                var role = guild.GetRole(id);
                if (role == null)
                {
                    builder.AppendLine($"Deleted Role: {id}");
                }
                else
                {
                    builder.AppendLine($"{role.Mention}");
                }
            }

            return builder.ToString();
        }
    }
}