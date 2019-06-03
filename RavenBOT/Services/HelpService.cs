using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Extensions;
using RavenBOT.Models;

namespace RavenBOT.Services
{
    public class HelpService
    {
        public PrefixService PrefixService { get; }
        public ModuleManagementService ModuleManager { get; }
        public CommandService CommandService { get; }
        public BotConfig Config { get; }
        public DeveloperSettings DeveloperSettings { get; }
        public IServiceProvider Provider { get; }

        public HelpService(PrefixService prefixService, ModuleManagementService moduleManager, CommandService cmdService, BotConfig config, DeveloperSettings developerSettings, IServiceProvider provider)
        {
            PrefixService = prefixService;
            ModuleManager = moduleManager;
            CommandService = cmdService;
            Config = config;
            DeveloperSettings = developerSettings;
            Provider = provider;
        }

        public async Task<PaginatedMessage> PagedHelpAsync(ShardedCommandContext context, bool usePreconditions = true, List<string> moduleFilter = null)
        {
            var commandCollection = CommandService.Commands.ToList();

            var modules = commandCollection.GroupBy(c => c.Module.Name).Select(x => new Tuple<string, List<CommandInfo>>(x.Key, x.ToList())).ToList(); 

            //Use filter out any modules that are not chosen in the filter.
            if (moduleFilter != null && moduleFilter.Any())
            {
                modules = modules.Where(x => moduleFilter.Any(f => x.Item1.Contains(f, StringComparison.InvariantCultureIgnoreCase))).ToList();
            }

            //Skip blacklisted modules
            var moduleConfig = ModuleManager.GetModuleConfig(context.Guild?.Id ?? 0);
            if (moduleConfig.Blacklist.Any())
            {
                //Filter out any blacklisted modules for the server
                modules = modules.Where(x => moduleConfig.Blacklist.All(bm => !x.Item1.Equals(bm, StringComparison.InvariantCultureIgnoreCase))).ToList();
            }

            if (usePreconditions)
            {
                if (modules.Sum(x => x.Item2.Count) > 20)
                {
                    await context.Channel.SendMessageAsync("This command filters out all commands that you do not have sufficient permissions to access. As such it may take a moment to generate.\n" +
                                                            "If you want to see every command, use the fullhelp command instead.");
                }

                //TODO: Test efficiency of this versus the other one, including precondition skips
                var newModules = new List<Tuple<string, List<CommandInfo>>>();
                for (int i = 0; i < modules.Count; i++)
                {
                    var module = modules[i];
                    var commands = new List<CommandInfo>();
                    foreach (var command in module.Item2)
                    {
                        if (await CheckPreconditionsAsync(context, command).ConfigureAwait(false))
                        {
                            commands.Add(command);
                        }                        
                    }
                    
                    if (commands.Any())
                    {
                        
                        newModules.Add(new Tuple<string, List<CommandInfo>>(module.Item1, commands));
                    }
                }

                modules = newModules;
            }

            modules = modules.OrderBy(m => m.Item1).ToList();

            var overviewFields = new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder
                {
                    // This gives a brief overview of how to use the paginated message and help commands.
                    Name = $"Commands Summary",
                    Value =
                        "Go to the respective page number of each module to view the commands in more detail. "
                        + "You can react with the :1234: emote and type a page number to go directly to that page too,\n"
                        + "otherwise react with the arrows (◀ ▶) to change pages.\n"
                }
            };
            foreach(var commandGroup in modules)
            {
                var moduleName = commandGroup.Item1;
                var commands = commandGroup.Item2;

                //This will be added to the 'overview' for each module
                var moduleContent = new StringBuilder();
                moduleContent.AppendJoin(", ", commands.Select(x => x.Name));

                //Handle modules with the same name and group them.
                var duplicateModule = overviewFields.FirstOrDefault(x => x.Name.Equals(moduleName, StringComparison.InvariantCultureIgnoreCase));
                if (duplicateModule != null)
                {
                    if (duplicateModule.Value is string value)
                    {
                        duplicateModule.Value = $"{value}\n{moduleContent.ToString()}".FixLength();
                    }
                }
                else
                {
                    overviewFields.Add(new EmbedFieldBuilder
                    {
                        Name = moduleName,
                        Value = moduleContent.ToString().FixLength()                
                    });
                }
            }

            int pageIndex = 0;
            var pages = new List<Tuple<int, PaginatedMessage.Page>>();
            foreach(var commandGroup in modules)
            {
                var moduleName = commandGroup.Item1;
                var commands = commandGroup.Item2;

                //This will be it's own page in the paginator
                var page = new PaginatedMessage.Page();
                var pageContent = new StringBuilder();
                var commandContent = new StringBuilder();
                foreach (var command in commands)
                {
                    commandContent.AppendLine($"**{command.Name}**");
                    if (command.Summary != null)
                    {
                        commandContent.AppendLine($"[Summary]{command.Summary}");
                    }
                    if (command.Remarks != null)
                    {
                        commandContent.AppendLine($"[Remarks]{command.Remarks}");
                    }
                    if (command.Preconditions.Any())
                    {
                        commandContent.AppendLine($"[Preconditions]{string.Join(" ", command.Preconditions.Select(x => x.GetType().Name))}");
                    }
                    if (command.Aliases.Count > 1)
                    {
                        commandContent.AppendLine($"[Aliases]{string.Join(",", command.Aliases)}");
                    }
                    commandContent.AppendLine($"`{command.Aliases.First()} {string.Join(" ", command.Parameters.Select(ParameterInformation))}`");
                
                    if (pageContent.Length + commandContent.Length > 2047)
                    {
                        page.Title = moduleName;
                        page.Description = pageContent.ToString();
                        pages.Add(new Tuple<int, PaginatedMessage.Page>(pageIndex, page));
                        pageIndex++;
                        page = new PaginatedMessage.Page();
                        pageContent.Clear();
                        pageContent.Append(commandContent.ToString());
                    }
                    else
                    {
                        pageContent.Append(commandContent.ToString());
                    }
                    commandContent.Clear();
                }
                
                page.Title = moduleName;
                page.Description = pageContent.ToString();
                pages.Add(new Tuple<int, PaginatedMessage.Page>(pageIndex, page));
                pageIndex++;
            }

            //This division will round upwards (overviewFields.Count - 1)/5 +1
            int overviewPageCount = ((overviewFields.Count - 1)/5) + 1;

            for (int i = 0; i < pages.Count; i++)
            {
                //Use indexing rather than foreach to avoid updating a collection while it is being interated
                pages[i] = new Tuple<int, PaginatedMessage.Page>(pages[i].Item1 + overviewPageCount + 1 /* Use + 1 as the pages are appended to the overview page count */, pages[i].Item2);
            }

            var overviewPages = new List<PaginatedMessage.Page>();
            foreach (var fieldGroup in overviewFields.SplitList(5))
            {
                overviewPages.Add(new PaginatedMessage.Page
                {
                    Fields = fieldGroup.Select(x =>
                    {
                        //Modify all overview names to include the page index for the complete summary
                        x.Name = $"[{pages.FirstOrDefault(p => p.Item2.Title.Equals(x.Name))?.Item1.ToString() ?? $"1-{overviewPageCount}"}] {x.Name}";
                        return x;
                    }).ToList()
                });
            }

            overviewPages.AddRange(pages.Select(x => x.Item2));
            var pager = new PaginatedMessage
            {
                Pages = overviewPages,
                Color = Color.Green,
                Title = $"{context.Client.CurrentUser.Username} Commands"
            };           
            pager.Pages = overviewPages;

            return pager;
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



        public async Task<bool> CheckPreconditionsAsync(ShardedCommandContext context, CommandInfo command)
        {
            var devSettings = DeveloperSettings.GetDeveloperSettings();

            foreach (var preconditon in command.Preconditions)
            {
                if (devSettings.SkippableHelpPreconditions.Contains(preconditon.GetType().Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                var result = await preconditon.CheckPermissionsAsync(context, command, Provider).ConfigureAwait(false);
                if (result.IsSuccess) continue;

                return false;
            }

            return true;
        }

        /*
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
            var module = CommandService.Modules.FirstOrDefault(x => string.Equals(x.Name, checkForMatch, StringComparison.InvariantCultureIgnoreCase));
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

            var moduleInfos = CommandService.Modules.ToList();

            if (specifiedModules != null)
            {
                if (specifiedModules.Any())
                {
                    moduleInfos = moduleInfos.Where(x => specifiedModules.Any(sm => x.Name.StartsWith(sm, true, CultureInfo.InvariantCulture))).ToList();
                    //modules = modules.Where(x => specifiedModules.Any(sm => x.Name.StartsWith(sm, true, CultureInfo.InvariantCulture))).ToList();
                }
            }

            var moduleConfig = ModuleManager.GetModuleConfig(context.Guild?.Id ?? 0);
            if (moduleConfig.Blacklist.Any())
            {
                //Filter out any blacklisted modules for the server
                moduleInfos = moduleInfos.Where(x => moduleConfig.Blacklist.All(bm => !x.Name.Equals(bm, StringComparison.InvariantCultureIgnoreCase))).ToList();
            }

            foreach (var module in moduleInfos)
            {
                modules.Add(await ParseModule(context, module, checkPreconditions));
            }

            modules = modules.Where(x => x.Commands.Any()).OrderBy(x => x.Name).ToList();


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

            //TODO: Handle command filters from modulemanagementservice

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
        */
    }
}
