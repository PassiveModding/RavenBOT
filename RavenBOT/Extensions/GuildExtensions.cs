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
    }
}
