using System.Collections.Generic;
using System.Text;
using Discord;

namespace RavenBOT.Extensions
{
    public static class GuildExtensions
    {
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
