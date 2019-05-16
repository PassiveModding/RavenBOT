using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Extensions;
using RavenBOT.Models;
using RavenBOT.Modules.Developer.Methods;

namespace RavenBOT.Services
{
    public class HelpService
    {
        public PrefixService PrefixService { get; }
        public CommandService CommandService { get; }
        public BotConfig Config { get; }
        public Setup Setup { get; }
        public IServiceProvider Provider { get; }

        public HelpService(PrefixService prefixService, CommandService cmdService, BotConfig config, Setup setup, IServiceProvider provider)
        {
            PrefixService = prefixService;
            CommandService = cmdService;
            Config = config;
            Setup = setup;
            Provider = provider;
        }

        public async Task<bool> TestPreconditions(ShardedCommandContext context, CommandInfo command)
        {
            var devSettings = Setup.GetDeveloperSettings();

            foreach (var preconditon in command.Preconditions)
            {
                if (devSettings.SkippableHelpPreconditions.Contains(preconditon.GetType().Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                var result = await preconditon.CheckPermissionsAsync(context, command, Provider);
                if (result.IsSuccess) continue;

                return false;
            }

            return true;
        }

        public async Task<List<CommandInfo>> GetPassingCommands(ShardedCommandContext context, ModuleInfo module)
        {
            var passingCommands = new List<CommandInfo>();
            foreach (var commandInfo in module.Commands)
            {
                if (await TestPreconditions(context, commandInfo))
                {
                    passingCommands.Add(commandInfo);
                }
            }

            return passingCommands;
        }

        public async Task<ReactionCallbackData> ModuleCommandHelpAsync(ShardedCommandContext context, bool checkPreconditions, string checkForMatch, CommandInfo currentCommand)
        {
            var module = CommandService.Modules.FirstOrDefault(x => string.Equals(x.Name, checkForMatch, StringComparison.CurrentCultureIgnoreCase));
            var fields = new List<EmbedFieldBuilder>();
            var pre = PrefixService.GetPrefix(context.Guild?.Id ?? 0);

            if (module != null)
            {
                List<CommandInfo> passingCommands;
                if (checkPreconditions)
                {
                    passingCommands = await GetPassingCommands(context, module);
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

            if (currentCommand != null)
            {
                var command = CommandService.Search(context, context.Message.Content.Substring(currentCommand.Aliases.First().Length + pre.Length + 1)).Commands?.FirstOrDefault().Command;
                if (command != null)
                {
                    if (command.CheckPreconditionsAsync(context, Provider).Result.IsSuccess)
                    {
                        fields.Add(new EmbedFieldBuilder { Name = $"Command: {command.Name}", Value = "**Usage:**\n" + $"{pre}{command.Aliases.FirstOrDefault()?.Replace($"{command.Module.Group} ", command.Module.Group)} {string.Join(" ", command.Parameters.Select(ParameterInformation))}\n" + "**Aliases:**\n" + $"{string.Join("\n", command.Aliases)}\n" + "**Module:**\n" + $"{command.Module.Name}\n" + "**Summary:**\n" + $"{command.Summary ?? "N/A"}\n" + "**Remarks:**\n" + $"{command.Remarks ?? "N/A"}" });
                    }
                }
            }


            if (!fields.Any())
            {
                await context.Channel.SendMessageAsync("There are no matches for this input.");
                return null;
            }

            return 
                new ReactionCallbackData(string.Empty, new EmbedBuilder { Fields = fields, Color = Color.DarkRed }.Build(), timeout: TimeSpan.FromMinutes(5)).WithCallback(
                    new Emoji("❌"),
                    async (c, r) =>
                        {
                            await r.Message.Value?.DeleteAsync();
                            await c.Message.DeleteAsync();
                        });
        }

        public async Task<ParsedModule> ParseModule(ShardedCommandContext context, ModuleInfo info, bool testCommands)
        {
            var parse = new ParsedModule
            {
                Commands = testCommands ? await GetPassingCommands(context, info) : info.Commands.ToList(),
                Name = info.Name,
                Summary = info.Summary
            };

            return parse;
        }

        public class ParsedModule
        {
            public List<CommandInfo> Commands { get; set; }
            public string Name { get; set; }
            public string Summary { get; set; }
        }


        public async Task<PaginatedMessage> PagedHelpAsync(ShardedCommandContext context, bool checkPreconditions, List<string> specifiedModules = null)
        {
            var pages = new List<PaginatedMessage.Page>();
            var moduleIndex = 1;
            //var pre = PrefixService.GetPrefix(context.Guild?.Id ?? 0);

            // This ensures that we filter out all modules where the user cannot access ANY commands
            var modules = new List<ParsedModule>();

            foreach (var module in CommandService.Modules)
            {
                modules.Add(await ParseModule(context, module, checkPreconditions));
            }

            modules = modules.Where(x => x.Commands.Any()).OrderBy(x => x.Name).ToList();

            if (specifiedModules != null)
            {
                if (specifiedModules.Any())
                {
                    modules = modules.Where(x => specifiedModules.Any(sm => x.Name.StartsWith(sm))).ToList();
                }
            }

            if (!modules.Any())
            {
                return null;
            }

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
                                             //+ "For more info on modules or commands,\n"
                                             //+ $"type `{pre}help <ModuleName>` or `{pre}help <CommandName>`"
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
                    fields.Add(new EmbedFieldBuilder { Name = $"[{moduleIndex}] {module.Name}", Value = string.Join(", ", module.Commands.Select(x => x.Aliases.FirstOrDefault()?.Replace($"{x.Module.Group} ", x.Module.Group)).Where(x => x != null).ToList()) });

                    try
                    {
                        // This gives us the prefix, command name and all parameters to the command.
                        var summary = module.Commands.Select(x => $"{(x.Summary == null ? "" : $"__**{x.Aliases.FirstOrDefault()?.Replace($"{x.Module.Group} ", x.Module.Group)} - {x.Summary}**__\n")}`{x.Aliases.FirstOrDefault()?.Replace($"{x.Module.Group} ", x.Module.Group)} {string.Join(" ", x.Parameters.Select(ParameterInformation))}`").ToList();

                        if (!string.IsNullOrEmpty(module.Summary))
                        {
                            summary.Add($"**Summary**\n{module.Summary}");
                        }

                        // Add a full page summary to our 'PageContents' list for later use
                        pageContents.TryAdd(module.Name, summary);
                    }
                    catch
                    {
                        // Note this should only throw IF there are two modules with the same name in the bot.
                        // LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                    }
                }

                // Add the page for each Module Set to our pages list.
                pages.Add(new PaginatedMessage.Page { Fields = fields, Title = $"{context.Client.CurrentUser.Username} Commands {setIndex}" });

                // Reset the fields list for the next module set
                fields = new List<EmbedFieldBuilder>();
                setIndex++;
            }

            // Now add each page with the full info with parameters 
            foreach (var contents in pageContents)
            {
                // Split these into groups of 10 to ensure there is no embed field character limit being hit. (1024 characters bet field description)
                var splitFields = contents.Value.SplitList(10).Select(x => new EmbedFieldBuilder { Name = contents.Key, Value = string.Join("\n", x).FixLength() }).ToList();
                pages.Add(new PaginatedMessage.Page { Fields = splitFields });
            }

            return new PaginatedMessage { Pages = pages, Title = $"{context.Client.CurrentUser.Username} Help", Color = Color.DarkRed };
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
    }
}
