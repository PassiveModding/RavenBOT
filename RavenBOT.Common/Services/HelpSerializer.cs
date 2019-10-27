using Discord.Commands;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace RavenBOT.Common
{
    public partial class HelpService
    {
        public class ModuleOverview
        {
            public string Name { get; set; }
            public List<PreconditionOverview> Preconditions { get; set; }
            public string Summary { get; set; }
            public string Remarks { get; set; }
            public string[] Aliases { get; set; }
            public List<CommandOverview> Commands { get; set; }
            public class CommandOverview
            {
                public string Name { get; set; }
                public string Summary { get; set; }
                public string Remarks { get; set; }
                public string[] Aliases { get; set; }
                public string Usage { get; set; }
                public string RunMode { get; set; }

                public List<PreconditionOverview> Preconditions { get; set; }

            }
        }

        public class PreconditionOverview
        {
            public string Name { get; set; }
            public string Summary { get; set; }
        }

        public PreconditionOverview GetPreconditionOverview(PreconditionAttribute precondition)
        {
            var overview = new PreconditionOverview();
            if (precondition is PreconditionBase preBase)
            {
                overview.Name = preBase.Name();
                overview.Summary = preBase.PreviewText();
            }
            else
            {
                overview.Name = precondition.GetType().Name;
                overview.Summary = null;
            }
            return overview;
        }

        public string GetModuleOverviewJson()
        {
            var overviews = CommandService.Modules.Select(module => new ModuleOverview
            {
                Name = module.Name,
                Aliases = module.Aliases.ToArray(),
                Summary = module.Summary,
                Remarks = module.Remarks,
                Preconditions = module.Preconditions.Select(p => GetPreconditionOverview(p)).ToList(),
                Commands = module.Commands.Select(x => new ModuleOverview.CommandOverview
                {
                    Name = x.Name,
                    Aliases = x.Aliases.ToArray(),
                    Summary = x.Summary,
                    Remarks = x.Remarks,
                    RunMode = x.RunMode.ToString(),
                    Usage = x.Parameters.ParameterUsage(),
                    Preconditions = x.Preconditions.Select(p => GetPreconditionOverview(p)).ToList()
                }).ToList()
            });

            return JsonConvert.SerializeObject(overviews, Formatting.Indented);
        }
    }
}