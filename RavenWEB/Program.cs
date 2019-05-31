using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RavenWEB
{
    public class Program
    {
        public static RavenBOT.Program RavenBOT { get; set; }
        public static string setupFolder = Path.Combine(AppContext.BaseDirectory, "Setup");
        public static string configFile = Path.Combine(setupFolder, "API_Config.json");

        public static void Main(string[] args)
        {
            RavenBOT = new RavenBOT.Program();
            var _ = Task.Run(() => RavenBOT.RunAsync());

            if (!Directory.Exists(setupFolder))
            {
                Directory.CreateDirectory(setupFolder);
            }

            if (!File.Exists(configFile))
            {
                File.WriteAllText(configFile, "{" +
                                              "\"Discord:AppSecret\":\"\"," +
                                              "\"Discord:AppId\":\"\"" +
                                              "\"Discord:RedirectUrl\":\"\"" +
                                              "}");
                Console.WriteLine($"App Config file generated at {configFile} please set your app secret and id\n" +
                                  $"----Press Any Key to Continue----");
                Console.ReadKey();
                return;
            }
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile(
                        configFile, optional: false, reloadOnChange: false);
                    config.AddCommandLine(args);
                })
                .UseStartup<Startup>();
    }
}
