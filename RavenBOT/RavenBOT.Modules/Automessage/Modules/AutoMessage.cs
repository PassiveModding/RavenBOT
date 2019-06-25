using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common.Attributes;
using RavenBOT.Common.Services;
using RavenBOT.Modules.Automessage.Methods;
using RavenBOT.Modules.Automessage.Models;

namespace RavenBOT.Modules.Automessage.Modules
{
    [Group("Automessage")]
    [RavenRequireContext(ContextType.Guild)]
    [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]
    [Summary("Handles automated messaging to specific discord channels")]
    public class AutoMessage : InteractiveBase<ShardedCommandContext>
    {
        public AutoMessage(AutomessageHandler automessageHandler, HelpService helper)
        {
            AutomessageHandler = automessageHandler;
            HelpService = helper;
        }

        public AutomessageHandler AutomessageHandler { get; }
        public HelpService HelpService { get; }

        [Command("Help")]
        public async Task HelpAsync()
        {
            var res = await HelpService.PagedHelpAsync(Context, true, new List<string>
            {
                "Automessage"
            }, "This module handles automated messages to the specified channels");

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

        [Command("AddChannel")]
        [Summary("Adds the current channel as an auto-message chanel")]
        public async Task AddAutoMessageChannelAsync([Summary("The amount of messages between each auto-message")] int messageCount, [Remainder] string message)
        {
            var config = AutomessageHandler.GetAutomessageChannel(Context.Channel.Id);

            if (config != null)
            {
                await ReplyAsync("Channel Already Exists.");
                return;
            }

            var newChannel = new AutomessageChannel(Context.Channel.Id)
            {
                Response = message,
                RespondOn = messageCount
            };

            AutomessageHandler.SaveAutomessageChannel(newChannel);
            await ReplyAsync("Channel Added.");
        }

        [Command("RemoveChannel")]
        [Summary("Removes the current channel from auto-messages")]
        public async Task RemoveAutoMessageChannelAsync()
        {
            if (AutomessageHandler.Cache.TryGetValue(Context.Channel.Id, out var config))
            {
                if (AutomessageHandler.RemoveAutomessageChannel(config.Value))
                {
                    await ReplyAsync("Channel Removed.");
                }
                else
                {
                    await ReplyAsync("Error removing channel, try again.");
                }
            }
            else
            {
                var dbConfig = AutomessageHandler.GetAutomessageChannel(Context.Channel.Id);
                if (dbConfig == null)
                {
                    await ReplyAsync("Done.");
                    return;
                }

                if (AutomessageHandler.RemoveAutomessageChannel(dbConfig))
                {
                    await ReplyAsync("Channel Removed.");
                }
                else
                {
                    await ReplyAsync("Error removing channel, try again.");
                }
            }
        }
    }
}