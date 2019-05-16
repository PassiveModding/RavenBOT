using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace RavenBOT.Modules.Lithium.Methods
{
    public partial class ModerationService
    {
        public bool CheckToxicityAsync(string message, int max)
        {
            if (Perspective == null)
            {
                return false;
            }

            try
            {
                var res = Perspective.QueryToxicity(message);
                if (res.attributeScores.TOXICITY.summaryScore.value * 100 > max)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }
        public async Task RunChecks(SocketUserMessage message, SocketTextChannel channel)
        {
            var guildSetup = GetModerationConfig(channel.Guild.Id);

            //TODO: Spam check

            if (guildSetup.BlockInvites)
            {
                if (Regex.IsMatch(message.Content, @"discord(?:\.gg|\.me|app\.com\/invite)\/([\w\-]+)", RegexOptions.IgnoreCase))
                {
                    await message.DeleteAsync();
                    return;
                }
            }

            if (guildSetup.BlockIps)
            {
                if (Regex.IsMatch(message.Content, @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"))
                {
                    await message.DeleteAsync();
                    return;
                }
            }

            if (guildSetup.BlockMassMentions)
            {
                int count = 0;
                if (guildSetup.MassMentionsIncludeChannels)
                {
                    count += message.MentionedChannels.Count;
                }

                if (guildSetup.MassMentionsIncludeRoles)
                {
                    count += message.MentionedRoles.Count;
                }

                if (guildSetup.MassMentionsIncludeUsers)
                {
                    count += message.MentionedUsers.Count;
                }

                if (count > guildSetup.MaxMentions)
                {
                    await message.DeleteAsync();
                    return;
                }
            }

            if (guildSetup.UseBlacklist)
            {
                var match = guildSetup.BlacklistCheck(message.Content);
                if (match.Item1)
                {
                    await message.DeleteAsync();
                    return;
                    //TODO: Log message, matched value, regex.
                }
            }

            if (guildSetup.UsePerspective)
            {
                //This is the last check because it utilises a web request which introduces more latency.
                var res = CheckToxicityAsync(message.Content, guildSetup.PerspectiveMax);
                if (res)
                {
                    await message.DeleteAsync();
                    //TODO: Log this action and maybe reply.
                }
            }
        }
    }
}