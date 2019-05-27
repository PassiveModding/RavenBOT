using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace RavenBOT.Modules.AutoMod.Methods
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
            if (string.IsNullOrWhiteSpace(message.Content))
            {
                return;
            }

            var gUser = channel.Guild.GetUser(message.Author.Id);
            if (gUser != null)
            {
                if (gUser.GuildPermissions.Administrator)
                {
                    //Do not run message checks for admins.
                    return;
                }
            }

            var guildSetup = GetModerationConfig(channel.Guild.Id);

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

            if (guildSetup.UseAntiSpam)
            {
                var spamResult = CheckSpam(guildSetup, message, channel, out var messages);
                if (spamResult == SpamType.RepetitiveMessage)
                {
                    await channel.DeleteMessagesAsync(messages.Select(x => x.MessageId));
                    if (messages.Count(x => x.Responded) == 1)
                    {
                        await channel.SendMessageAsync($"{message.Author.Mention} no spamming! (Too many identical messages)");
                        //TODO: Log
                    }
                    return;
                }
                else if (spamResult == SpamType.TooFast)
                {
                    await channel.DeleteMessagesAsync(messages.Select(x => x.MessageId));
                    if (messages.Count(x => x.Responded) == 1)
                    {
                        await channel.SendMessageAsync($"{message.Author.Mention} no spamming! (Too many messages over specified time)");
                        //TODO: Log
                    }
                    return;
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