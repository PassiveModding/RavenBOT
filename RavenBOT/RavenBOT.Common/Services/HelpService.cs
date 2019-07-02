using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using MoreLinq;

namespace RavenBOT.Common
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

        public async Task<PaginatedMessage> PagedHelpAsync(ShardedCommandContext context, bool usePreconditions = true, List<string> moduleFilter = null, string additionalField = null)
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

                var newModules = new List<Tuple<string, List<CommandInfo>>>();
                var devSettings = DeveloperSettings.GetDeveloperSettings();
                for (int i = 0; i < modules.Count; i++)
                {
                    var module = modules[i];
                    var commands = new List<CommandInfo>();
                    foreach (var command in module.Item2)
                    {
                        if (await CheckPreconditionsAsync(context, command, devSettings))
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
                "Go to the respective page number of each module to view the commands in more detail. " +
                "You can react with the :1234: emote and type a page number to go directly to that page too,\n" +
                "otherwise react with the arrows (◀ ▶) to change pages.\n" +
                additionalField
                }
            };

            foreach (var commandGroup in modules)
            {
                var moduleName = commandGroup.Item1;
                var commands = commandGroup.Item2;

                //This will be added to the 'overview' for each module
                var moduleContent = new StringBuilder();
                moduleContent.AppendJoin(", ", commands.GroupBy(x => x.Name).Select(x => x.First().Aliases.First()));

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
            foreach (var commandGroup in modules)
            {
                var moduleName = commandGroup.Item1;
                var commands = commandGroup.Item2;

                //This will be it's own page in the paginator
                var page = new PaginatedMessage.Page();
                var pageContent = new StringBuilder();
                var commandContent = new StringBuilder();
                foreach (var command in commands)
                {
                    commandContent.AppendLine($"**{command.Name ?? command.Module.Aliases.FirstOrDefault()}**");
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

                        commandContent.AppendLine($"[Preconditions]\n{GetPreconditionSummaries(command.Preconditions)}");
                    }
                    if (command.Aliases.Count > 1)
                    {
                        commandContent.AppendLine($"[Aliases]{string.Join(",", command.Aliases)}");
                    }
                    commandContent.AppendLine($"`{command.Aliases.First() ?? command.Module.Aliases.FirstOrDefault()} {string.Join(" ", command.Parameters.Select(x => x.ParameterInformation()))}`");

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
                //TODO: Add module specific preconditions in additional field
                //Otherwise apply those preconditions to all relevant command precondition descriptions

                var modPreconditons = commandGroup.Item2.SelectMany(x => x.Module.Preconditions).DistinctBy(x => (x as PreconditionBase)?.PreviewText() ?? x.GetType().ToString());
                if (modPreconditons.Any())
                {
                    page.Fields.Add(new EmbedFieldBuilder
                    {
                        Name = "Module Preconditions", 
                        Value = GetPreconditionSummaries(modPreconditons) ?? "N/A"
                    });
                }

                pages.Add(new Tuple<int, PaginatedMessage.Page>(pageIndex, page));
                pageIndex++;
            }

            //This division will round upwards (overviewFields.Count - 1)/5 +1
            int overviewPageCount = ((overviewFields.Count - 1) / 5) + 1;

            for (int i = 0; i < pages.Count; i++)
            {
                //Use indexing rather than foreach to avoid updating a collection while it is being interated
                pages[i] = new Tuple<int, PaginatedMessage.Page>(pages[i].Item1 + overviewPageCount + 1 /* Use + 1 as the pages are appended to the overview page count */ , pages[i].Item2);
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

        public string GetPreconditionSummaries(IEnumerable<PreconditionAttribute> preconditions)
        {
            var preconditionString = string.Join("\n", preconditions.Select(x => 
                {
                    if (x is PreconditionBase preBase)
                    {
                        return $"__{preBase.Name()}__ {preBase.PreviewText()}";
                    }
                    else
                    {
                        return x.GetType().Name;
                    }
                }).Distinct().ToArray()).FixLength();

            return preconditionString;
        }

        public async Task<bool> CheckPreconditionsAsync(ShardedCommandContext context, CommandInfo command, DeveloperSettings.Settings settings)
        {
            var preconditions = new List<PreconditionAttribute>();
            preconditions.AddRange(command.Preconditions);
            preconditions.AddRange(command.Module.Preconditions);
            foreach (var precondition in preconditions)
            {
                if (settings.SkippableHelpPreconditions.Contains(precondition.GetType().Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                var result = await precondition.CheckPermissionsAsync(context, command, Provider);
                if (result.IsSuccess)
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}