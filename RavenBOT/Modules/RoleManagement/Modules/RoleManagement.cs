using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using MoreLinq;
using RavenBOT.Modules.RoleManagement.Methods;
using RavenBOT.Modules.RoleManagement.Models;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.RoleManagement.Modules
{
    [Group("RoleManager")]
    [RequireContext(ContextType.Guild)]
    public class RoleManagement : InteractiveBase<ShardedCommandContext>
    {
        public RoleManagement(RoleManager manager)
        {
            Manager = manager;
        }

        public IDatabase Database { get; }
        public RoleManager Manager { get; }

        [Command("CreateMessage")]
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