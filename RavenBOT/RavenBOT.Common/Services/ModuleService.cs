using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;

namespace RavenBOT.Common.Services
{
    public class ModuleService
    {
        public ModuleService(CommandService commandService, IServiceProvider provider)
        {
            CommandService = commandService;
            Provider = provider;
        }

        public CommandService CommandService { get; }
        public IServiceProvider Provider { get; }

        public async Task<IEnumerable<ModuleInfo>> RegisterModule(string dllPath)
        {

            var reflectedDll = Assembly.LoadFile(dllPath);

            AppDomain currentDomain = AppDomain.CurrentDomain;

            var probeDirectory = Directory.GetParent(dllPath).FullName;
            var assemblies = reflectedDll.GetReferencedAssemblies().Select(x => x.Name);
            var currentDirectory = AppContext.BaseDirectory;
            foreach (var assembly in assemblies)
            {
                var path = Path.Combine(probeDirectory, assembly + ".dll");
                var destination = Path.Combine(currentDirectory, assembly + ".dll");
                if (File.Exists(path) && !File.Exists(destination))
                {
                    //Assembly.LoadFile(path);
                }
            }

            var res = await CommandService.AddModulesAsync(reflectedDll, Provider);
            return res;
        }
    }
}