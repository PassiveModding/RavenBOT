using System;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Methods;

namespace RavenBOT.ELO.Modules.Modules
{
    public class UserCommands : InteractiveBase<ShardedCommandContext>
    {
        public ELOService Service { get; }

        public UserCommands(ELOService service)
        {
            Service = service;
        }

        [Command("Register", RunMode = RunMode.Sync)]
        [Alias("reg")]
        public async Task RegisterAsync([Remainder]string name = null)
        {
            if (name == null)
            {
                name = Context.User.Username;
            }

            //TODO: Add option to prevent re-registering
            //TODO: Add precondition for premium  
            //TODO: Fix name not being set when re-registering

            var player = Service.GetPlayer(Context.Guild.Id, Context.User.Id) ?? Service.CreatePlayer(Context.Guild.Id, Context.User.Id, name);
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            var responses = await Service.UpdateUserAsync(competition, player, Context.User as SocketGuildUser);
            await ReplyAsync($"You have registered as `{name}`, all roles/name updates have been applied if applicable.");
            if (responses.Count > 0)
            {
                await ReplyAndDeleteAsync("", false, String.Join("\n", responses).QuickEmbed(), TimeSpan.FromSeconds(30));
            }
        }

        [Command("Rename", RunMode = RunMode.Sync)]
        public async Task RenameAsync([Remainder]string name = null)
        {
            if (name == null)
            {
                await ReplyAsync("You must specify a new name in order to be renamed.");
                return;
            }

            var player = Service.GetPlayer(Context.Guild.Id, Context.User.Id);
            if (player == null)
            {
                await ReplyAsync("You are not registered yet.");
                return;
            }

            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            
            var originalDisplayName = player.DisplayName;
            player.DisplayName = name;
            var newName = competition.GetNickname(player);

            var gUser = (Context.User as SocketGuildUser);
            var currentName = gUser.Nickname ?? gUser.Username;
            if (!currentName.Equals(newName))
            {
                if (gUser.Hierarchy < Context.Guild.CurrentUser.Hierarchy)
                {
                    if (Context.Guild.CurrentUser.GuildPermissions.ManageNicknames)
                    {
                        await gUser.ModifyAsync(x => x.Nickname = newName);
                    }
                    else
                    {
                        await ReplyAsync("The bot does not have the `ManageNicknames` permission and therefore cannot update your nickname.");
                    }
                }
                else
                {
                    await ReplyAsync("You have a higher permission level than the bot and therefore it cannot update your nickname.");
                }
            }

            Service.SavePlayer(player);
            await ReplyAsync($"Your profile has been renamed from {originalDisplayName} to {name}");            
        }
    }
}