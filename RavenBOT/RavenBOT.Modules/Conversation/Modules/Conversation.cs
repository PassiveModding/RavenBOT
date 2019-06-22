using System.Reflection;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common.Attributes;
using RavenBOT.Modules.Conversation.Methods;
using RavenBOT.Modules.Conversation.Models;

namespace RavenBOT.Modules.Conversation.Modules
{
    [Group("Conversation")]
    [RavenRequireOwner]
    [Remarks("Requires bot owner permissions")]
    public class Conversation : InteractiveBase<ShardedCommandContext>
    {
        public Conversation(ConversationService service)
        {
            Service = service;
        }

        public ConversationService Service { get; }

        [Command("SetConfigValue")]
        [Summary("Sets the google cloud authentication json for dialogflow")]
        public async Task SetConfigJson(string value, [Remainder] string content)
        {
            var currentConfig = Service.Database.Load<ConversationConfig>(ConversationConfig.DocumentName()) ?? new ConversationConfig();
            PropertyInfo prop = currentConfig.Certificate.GetType().GetProperty(value, BindingFlags.Public | BindingFlags.Instance);
            if (null != prop && prop.CanWrite)
            {
                prop.SetValue(currentConfig.Certificate, content, null);
            }

            Service.Database.Store(currentConfig, ConversationConfig.DocumentName());
            await ReplyAsync("Set.");
        }

        /*
        [Command("SetAgentName")]
        [Summary("Sets the dialogflow agent name")]
        public async Task SetAgentName(string name)
        {
            var currentConfig = Service.Database.Load<ConversationConfig>(ConversationConfig.DocumentName()) ?? new ConversationConfig();
            currentConfig.AgentName = name;
            Service.Database.Store(currentConfig, ConversationConfig.DocumentName());
            await ReplyAsync("Set.");
        }
        */

        [Command("Initialize")]
        [Summary("Attempts to initialize the dialogflow agent")]
        public async Task InitializeAgent()
        {
            Service.SetAgent();

            await ReplyAsync($"Enabled: {Service.IsEnabled()}");
        }

        [Command("ShowFunctions")]
        [Summary("Displays custom functions invokable by dialogflow responses")]
        public async Task ShowFunctions()
        {
            var fn = new ConversationFunctions();
            await ReplyAsync(string.Join("\n", fn.GetFunctions()));

            ConversationFunctions.ConversationResponse response = null;
            if (fn.TryInvoke("GetWeather", ref response))
            {
                await ReplyAsync(response.Value);
            }
        }
    }
}