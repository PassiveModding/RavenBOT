using System;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Methods;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    public class UserCommands : ReactiveBase
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

            //TODO: Add precondition for premium  
            
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            if (Context.User.IsRegistered(Service, out var player))
            {
                if (!competition.AllowReRegister)
                {
                    await ReplyAsync("You are not allowed to re-register.");
                    return;
                }
            }
            else
            {
                player = Service.CreatePlayer(Context.Guild.Id, Context.User.Id, name);
            }

            player.DisplayName = name;

            var responses = await Service.UpdateUserAsync(competition, player, Context.User as SocketGuildUser);

            await ReplyAsync(competition.FormatRegisterMessage(player));
            if (responses.Count > 0)
            {
                await SimpleEmbedAsync(string.Join("\n", responses));
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
            if (competition.UpdateNames && !currentName.Equals(newName))
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