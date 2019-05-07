using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Extensions;
using RavenBOT.Modules.Developer;
using RavenBOT.Modules.Developer.Methods;
using RavenBOT.Services;

namespace RavenBOT.Modules
{
    public class Info : InteractiveBase<ShardedCommandContext>
    {
        public CommandService CommandService { get; }
        public PrefixService PrefixService { get; }
        public IServiceProvider Provider { get; }
        public Setup Setup { get; }

        private Info(CommandService commandService, PrefixService prefixService, IServiceProvider provider)
        {
            CommandService = commandService;
            PrefixService = prefixService;
            Provider = provider;
            Setup = new Setup(provider.GetRequiredService<DatabaseService>().GetStore());
        }

        [Command("Invite")]
        public async Task InviteAsync()
        {
            await ReplyAsync($"Invite: https://discordapp.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot&permissions=2146958591");
        }

        [Command("Help")]
        public async Task HelpAsync([Remainder]string moduleOrCommand = null)
        {
            await GenerateHelpAsync(moduleOrCommand, true);
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
                    await PagedHelpAsync(checkPreconditions);
                }
                else
                {
                    await ModuleCommandHelpAsync(checkPreconditions, checkForMatch);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task<bool> TestPreconditions(CommandInfo command)
        {
            var devSettings = Setup.GetDeveloperSettings();

            foreach (var preconditon in command.Preconditions)
            {
                if (devSettings.SkippableHelpPreconditions.Contains(preconditon.GetType().Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                var result = await preconditon.CheckPermissionsAsync(Context, command, Provider);
                if (result.IsSuccess) continue;

                return false;
            }

            return true;
        }

        public async Task<List<CommandInfo>> GetPassingCommands(ModuleInfo module)
        {
            var passingCommands = new List<CommandInfo>();
            foreach (var commandInfo in module.Commands)
            {
                if (await TestPreconditions(commandInfo))
                {
                    passingCommands.Add(commandInfo);
                }
            }

            return passingCommands;
        }

        public async Task ModuleCommandHelpAsync(bool checkPreconditions, string checkForMatch)
        {
            var module = CommandService.Modules.FirstOrDefault(x => string.Equals(x.Name, checkForMatch, StringComparison.CurrentCultureIgnoreCase));
            var fields = new List<EmbedFieldBuilder>();
            var pre = PrefixService.GetPrefix(Context.Guild?.Id ?? 0);

            if (module != null)
            {
                List<CommandInfo> passingCommands;
                if (checkPreconditions)
                {
                    passingCommands = await GetPassingCommands(module);
                }
                else
                {
                    passingCommands = module.Commands.ToList();
                }

                if (!passingCommands.Any())
                {
                    throw new Exception("No Commands available with your current permission level.");
                }

                var info = passingCommands.Select(x => $"{pre}{x.Aliases.FirstOrDefault()} {string.Join(" ", x.Parameters.Select(ParameterInformation))}").ToList();
                var splitFields = info.SplitList(10).Select(x => new EmbedFieldBuilder { Name = $"Module: {module.Name}", Value = string.Join("\n", x) }).ToList();
                fields.AddRange(splitFields);
            }

            var command = CommandService.Search(Context, Context.Message.Content.Substring(Command.Aliases.First().Length + pre.Length + 1)).Commands?.FirstOrDefault().Command;
            if (command != null)
            {
                if (command.CheckPreconditionsAsync(Context, Provider).Result.IsSuccess)
                {
                    fields.Add(new EmbedFieldBuilder { Name = $"Command: {command.Name}", Value = "**Usage:**\n" + $"{pre}{command.Aliases.FirstOrDefault()} {string.Join(" ", command.Parameters.Select(ParameterInformation))}\n" + "**Aliases:**\n" + $"{string.Join("\n", command.Aliases)}\n" + "**Module:**\n" + $"{command.Module.Name}\n" + "**Summary:**\n" + $"{command.Summary ?? "N/A"}\n" + "**Remarks:**\n" + $"{command.Remarks ?? "N/A"}" });
                }
            }

            if (!fields.Any())
            {
                throw new Exception("There are no matches for this input.");
            }

            await InlineReactionReplyAsync(
                new ReactionCallbackData(string.Empty, new EmbedBuilder { Fields = fields, Color = Color.DarkRed }.Build(), timeout: TimeSpan.FromMinutes(5)).WithCallback(
                    new Emoji("❌"),
                    async (c, r) =>
                        {
                            await r.Message.Value?.DeleteAsync();
                            await c.Message.DeleteAsync();
                        }));
        }

        public async Task<ParsedModule> ParseModule(ModuleInfo info, bool testCommands)
        {
            var parse = new ParsedModule();
            parse.commands = testCommands ? await GetPassingCommands(info) : info.Commands.ToList();
            parse.Name = info.Name;
            parse.Summary = info.Summary;

            return parse;
        }

        public class ParsedModule
        {
            public List<CommandInfo> commands { get; set; }
            public string Name { get; set; }
            public string Summary { get; set; }
        }


        public async Task PagedHelpAsync(bool checkPreconditions)
        {
            var pages = new List<PaginatedMessage.Page>();
            var moduleIndex = 1;
            var pre = PrefixService.GetPrefix(Context.Guild?.Id ?? 0);

            // This ensures that we filter out all modules where the user cannot access ANY commands
            var modules = new List<ParsedModule>();

            foreach (var module in CommandService.Modules)
            {
                modules.Add(await ParseModule(module, checkPreconditions));
            }

            modules = modules.Where(x => x.commands.Any()).OrderBy(x => x.Name).ToList();

            // Split the modules into groups of 5 to ensure the message doesn't get too long
            var moduleSets = modules.SplitList(5);
            moduleIndex += moduleSets.Count - 1;
            var fields = new List<EmbedFieldBuilder>
                             {
                                 new EmbedFieldBuilder
                                     {
                                         // This gives a brief overview of how to use the paginated message and help commands.
                                         Name = $"[1-{moduleIndex}] Commands Summary",
                                         Value =
                                             "Go to the respective page number of each module to view the commands in more detail. "
                                             + "You can react with the :1234: emote and type a page number to go directly to that page too,\n"
                                             + "otherwise react with the arrows (◀ ▶) to change pages.\n"
                                             + "For more info on modules or commands,\n"
                                             + $"type `{pre}help <ModuleName>` or `{pre}help <CommandName>`"
                                     }
                             };

            var pageContents = new ConcurrentDictionary<string, List<string>>();
            var setIndex = 1;

            foreach (var moduleSet in moduleSets)
            {
                // Go through each module (in the sets of 5)
                foreach (var module in moduleSet)
                {
                    moduleIndex++;

                    // Add a new embed field with the info about our module and a list of all the command names
                    fields.Add(new EmbedFieldBuilder { Name = $"[{moduleIndex}] {module.Name}", Value = string.Join(", ", module.commands.Select(x => x.Aliases.FirstOrDefault()).Where(x => x != null).ToList()) });

                    try
                    {
                        // This gives us the prefix, command name and all parameters to the command.
                        var summary = module.commands.Select(x => $"{(x.Summary == null ? "" : $"__**{x.Aliases.FirstOrDefault()} - {x.Summary}**__\n")}`{pre}{x.Aliases.FirstOrDefault()} {string.Join(" ", x.Parameters.Select(ParameterInformation))}`").ToList();

                        if (!string.IsNullOrEmpty(module.Summary))
                        {
                            summary.Add($"**Summary**\n{module.Summary}");
                        }

                        // Add a full page summary to our 'PageContents' list for later use
                        pageContents.TryAdd(module.Name, summary);
                    }
                    catch (Exception e)
                    {
                        // Note this should only throw IF there are two modules with the same name in the bot.
                        // LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                    }
                }

                // Add the page for each Module Set to our pages list.
                pages.Add(new PaginatedMessage.Page { Fields = fields, Title = $"{Context.Client.CurrentUser.Username} Commands {setIndex}" });

                // Reset the fields list for the next module set
                fields = new List<EmbedFieldBuilder>();
                setIndex++;
            }

            // Now add each page with the full info with parameters 
            foreach (var contents in pageContents)
            {
                // Split these into groups of 10 to ensure there is no embed field character limit being hit. (1024 characters bet field description)
                var splitFields = contents.Value.SplitList(10).Select(x => new EmbedFieldBuilder { Name = contents.Key, Value = string.Join("\n", x) }).ToList();
                pages.Add(new PaginatedMessage.Page { Fields = splitFields });
            }

            await PagedReplyAsync(new PaginatedMessage { Pages = pages, Title = $"{Context.Client.CurrentUser.Username} Help | Prefix: {pre}", Color = Color.DarkRed }, new ReactionList { Backward = true, Forward = true, Jump = true, Trash = true });
        }

        public string ParameterInformation(ParameterInfo parameter)
        {
            var initial = parameter.Name + (parameter.Summary == null ? "" : $"({parameter.Summary})");
            var isAttributed = false;
            if (parameter.IsOptional)
            {
                initial = $"[{initial} = {parameter.DefaultValue ?? "null"}]";
                isAttributed = true;
            }

            if (parameter.IsMultiple)
            {
                initial = $"|{initial}|";
                isAttributed = true;
            }

            if (parameter.IsRemainder)
            {
                initial = $"...{initial}";
                isAttributed = true;
            }

            if (!isAttributed)
            {
                initial = $"<{initial}>";
            }

            return initial;
        }

        private CommandInfo Command { get; set; }

        protected override void BeforeExecute(CommandInfo command)
        {
            Command = command;
            base.BeforeExecute(command);
        }
    }
}
