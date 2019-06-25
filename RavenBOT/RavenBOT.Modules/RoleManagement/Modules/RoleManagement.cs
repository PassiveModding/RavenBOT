using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using MoreLinq;
using RavenBOT.Common.Attributes;
using RavenBOT.Common.Services;
using RavenBOT.Extensions;
using RavenBOT.Modules.RoleManagement.Methods;
using RavenBOT.Modules.RoleManagement.Models;

namespace RavenBOT.Modules.RoleManagement.Modules
{
    [Group("RoleManager")]
    [RavenRequireContext(ContextType.Guild)]
    [RavenRequireBotPermission(GuildPermission.ManageRoles)]    
    public class RoleManagement : InteractiveBase<ShardedCommandContext>
    {
        public RoleManagement(RoleManager manager, HelpService helpService)
        {
            Manager = manager;
            HelpService = helpService;
        }

        public RoleManager Manager { get; }
        public HelpService HelpService { get; }

        [Command("Help")]
        public async Task HelpAsync()
        {
            var res = await HelpService.PagedHelpAsync(Context, true, new List<string>
            {
                "rolemanager"
            }, "This module handles the giving of roles to users through message reactions and youtube subscriptions");

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

        [Command("CreateMessage")]
        [Summary("Creates an embedded message which users can react to in order to receive the specified role.")]
        [RavenRequireUserPermission(GuildPermission.Administrator)]    
        public async Task RoleMessageAsync(params IRole[] roles)
        {
            if (!roles.Any())
            {
                await ReplyAsync("You must specify roles to use.");
                return;
            }

            roles = roles.DistinctBy(x => x.Id).OrderBy(x => x.Name).ToArray();

            if (roles.Count() > 9)
            {
                await ReplyAsync("The maximum amount of roles for a managed embed is 9.");
                return;
            }

            var config = Manager.GetOrCreateConfig(Context.Guild.Id);
            var newMessage = new RoleConfig.RoleManagementEmbed
            {
                Roles = roles.Select(x => x.Id).ToList()
            };

            var lines = new List<string>();
            for (int i = 0; i < roles.Count(); i++)
            {
                lines.Add($":{numberedEmotes[i]}: {roles[i].Mention}");
            }

            var embed = new EmbedBuilder
            {
                Description = string.Join("\n", lines),
                Color = Color.Blue
            };

            var messageToUse = await ReplyAsync("", false, embed.Build());

            for (int i = 0; i < roles.Count(); i++)
            {
                await messageToUse.AddReactionAsync(new Emoji($"{i + 1}\U000020e3"));
            }

            newMessage.MessageId = messageToUse.Id;
            config.RoleMessages.Add(newMessage);
            Manager.SaveConfig(config);
            await ReplyAsync("Message created.");
        }

        [Command("Youtube Example")]
        [Summary("Verification info about the youtube subcription verification command.")]
        public async Task YoutubeExample()
        {
            var content = $"To verify your subscription status use the `verify subscription` command, followed by the display name of the channel you subscribed to and your own youtube channel id.\n" +
                $"eg. `verify subscription PassiveModding UCSEd2z_QfxQ_GJpDAAsvs4A`\n" +
                $"To find your channel ID, visit your channel and check the url for the following content:\n" +
                $"http://discord.passivenation.com/co3a0b1a2143.png\n" +
                $"NOTE: You must have your youtube subscriptions public when authenticating.\n" +
                "You can make them public by following the tutorial here:\n" +
                "https://support.google.com/youtube/answer/7280190?hl=en";
            await ReplyAsync("", false, content.QuickEmbed());
        }

        [Command("Sub Channels")]
        [Summary("Displays all configured youtube sub channels")]
        public async Task SubChannel()
        {
            var config = Manager.GetYTConfig(Context.Guild.Id);
            if (config == null || !config.SubRewards.Any())
            {
                await ReplyAsync("There are no sub roles configured.");
                return;
            }

            var content = string.Join("\n", config.SubRewards.Select(x => $"`{x.Key}` - https://www.youtube.com/channel/{x.Value.YoutubeChannelId}"));
            await ReplyAsync("", false, content.QuickEmbed());
        }

        [Command("Verify Subscription")]
        public async Task SubRoleRemove(string displayName, string userChannelId)
        {
            var config = Manager.GetYTConfig(Context.Guild.Id);
            if (config == null)
            {
                await ReplyAsync("There are no sub roles configured.");
                return;
            }

            if (!config.SubRewards.TryGetValue(displayName, out var channelConfig))
            {
                await ReplyAsync("The specified youtube channel is not a sub channel.");
                return;
            }

            var reAuthenticating = false;
            if (channelConfig.AuthenticatedUserIds.Any(x => x.UserId == Context.User.Id))
            {
                await ReplyAsync("You've already authenticated yourself as a subscriber to this person's channel. Reauthenticating...");
                reAuthenticating = true;
            }
            else if (channelConfig.AuthenticatedUserIds.Any(x => x.YoutubeChannelId.Equals(userChannelId, StringComparison.InvariantCultureIgnoreCase)))
            {
                //Ensure that a user hasn't already used the specified channel to verify their subscription
                await ReplyAsync("Another user has already used this channel to authenticate their subscription status.");
                return;
            }

            var response = await Manager.IsSubscribedTo(userChannelId, channelConfig.YoutubeChannelId);

            var role = Context.Guild.GetRole(channelConfig.RewardedRoleId);
            if (role == null)
            {
                return;
            }

            var gUser = Context.User as SocketGuildUser;

            switch (response)
            {
                case RoleManager.SubscriptionStatus.Error:
                    await ReplyAsync("There was an error configuring the subscription status.\n" +
                        "This may be because your youtube subscriptions are private. You can make them public by following the tutorial here:\n" +
                        "https://support.google.com/youtube/answer/7280190?hl=en");
                    await gUser.RemoveRoleAsync(role);
                    return;
                case RoleManager.SubscriptionStatus.NotSubscribed:
                    await ReplyAsync("You are not subscribed.");
                    await gUser.RemoveRoleAsync(role);
                    return;
                case RoleManager.SubscriptionStatus.Unknown:
                    await ReplyAsync("There was an error confirming your subscription status.");
                    await gUser.RemoveRoleAsync(role);
                    return;
            }

            await gUser.AddRoleAsync(role);

            if (!reAuthenticating)
            {
                channelConfig.AuthenticatedUserIds.Add(new YoutubeRoleConfig.SubReward.YoutubeSubscriber
                {
                    UserId = Context.User.Id,
                        YoutubeChannelId = userChannelId
                });
                Manager.SaveYTConfig(config);
            }

            var embed = $"You have been authenticated.".QuickEmbed();
            await ReplyAsync("", false, embed);
        }

        [Command("RemoveYoutubeSub")]
        [RavenRequireUserPermission(GuildPermission.Administrator)]    
        public async Task SubRoleRemove(string displayName)
        {
            var config = Manager.GetYTConfig(Context.Guild.Id);
            if (config == null)
            {
                await ReplyAsync("There are no sub roles configured.");
                return;
            }

            if (!config.SubRewards.Keys.Contains(displayName))
            {
                await ReplyAsync("The specified youtube channel is not a sub channel.");
                return;
            }

            config.SubRewards.Remove(displayName);

            Manager.SaveYTConfig(config);
            var embed = $"Channel Removed.".QuickEmbed();
            await ReplyAsync("", false, embed);
        }

        [Command("SetYoutubeSub")]
        [RavenRequireUserPermission(GuildPermission.Administrator)]    
        public async Task SubRoleCreate(string displayName, string subChannelId, IRole role)
        {
            var config = Manager.GetOrCreateYTConfig(Context.Guild.Id);

            if (config.SubRewards.Keys.Contains(displayName))
            {
                await ReplyAsync("Channel config already created. Run the removal command first.");
                return;
            }

            config.SubRewards.Add(displayName, new YoutubeRoleConfig.SubReward
            {
                DisplayName = displayName,
                    YoutubeChannelId = subChannelId,
                    RewardedRoleId = role.Id
            });

            Manager.SaveYTConfig(config);
            var embed = $"Youtube sub reward enabled. Note, for users to authenticate themselves\nthey must make their subscriptions public for youtube\nto do so, let them visit the following link: https://support.google.com/youtube/answer/7280190?hl=en\nUser verificaion is done using the `verify subscription` command and specifying the display name and their youtube channel id. ie. `verify subsription {displayName} UCSEd2z_QfxQ_GJpDAAsvs4A`".QuickEmbed();
            await ReplyAsync("", false, embed);
        }

        [Command("SetYoutubeApiKey")]
        [Summary("Set the youtube api key for checking the subscription status of users.")]
        [RavenRequireOwner]    
        public async Task SetYoutubeApiKeyAsync([Remainder] string key)
        {
            var config = Manager.Database.Load<YoutubeConfig>(YoutubeConfig.DocumentName());
            if (config == null)
            {
                config = new YoutubeConfig();
                config.ApiKey = key;
            }

            Manager.Database.Store(config, YoutubeConfig.DocumentName());
            await ReplyAsync("Key set.");
        }

        public string[] numberedEmotes = new string[]
        {
            "one",
            "two",
            "three",
            "four",
            "five",
            "six",
            "seven",
            "eight",
            "nine"
        };
    }
}