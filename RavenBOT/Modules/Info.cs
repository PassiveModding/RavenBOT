using System;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Models;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules
{
    [Group("info.")]
    public class Info : InteractiveBase<ShardedCommandContext>
    {
        public CommandService CommandService { get; }
        public PrefixService PrefixService { get; }
        public HelpService HelpService { get; }
        public IServiceProvider Provider { get; }
        public DeveloperSettings DeveloperSettings { get; }

        private Info(CommandService commandService, PrefixService prefixService, HelpService helpService, IServiceProvider provider)
        {
            CommandService = commandService;
            PrefixService = prefixService;
            HelpService = helpService;
            Provider = provider;
            DeveloperSettings = new DeveloperSettings(provider.GetRequiredService<IDatabase>());
        }

        [Command("Invite")]
        public async Task InviteAsync()
        {
            await ReplyAsync($"Invite: https://discordapp.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot&permissions=2146958591");
        }

        [Command("Help")]
        public async Task HelpAsync([Remainder]string moduleOrCommand = null)
        {
            await GenerateHelpAsync(moduleOrCommand);
        }

        [Command("FullHelp")]
        public async Task FullHelpAsync([Remainder][Summary("The name of a specific module or command")]string moduleOrCommand = null)
        {
            await GenerateHelpAsync(moduleOrCommand, false);
        }

        public async Task GenerateHelpAsync(string checkForMatch = null, bool checkPreconditions = true)
        {
            try
            {
                if (checkForMatch == null)
                {
                    var res = await HelpService.PagedHelpAsync(Context, checkPreconditions);
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
                }
                else
                {
                    var res = await HelpService.ModuleCommandHelpAsync(Context, checkPreconditions, checkForMatch, Command);
                    if (res != null)
                    {
                        await InlineReactionReplyAsync(res);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private CommandInfo Command { get; set; }

        protected override void BeforeExecute(CommandInfo command)
        {
            Command = command;
            base.BeforeExecute(command);
        }
    }
}
