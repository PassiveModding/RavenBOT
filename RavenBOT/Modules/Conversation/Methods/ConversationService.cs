using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Dialogflow.V2;
using Grpc.Auth;
using RavenBOT.Modules.Conversation.Models;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Conversation.Methods
{
    public class ConversationService : IServiceable
    {
        public ConversationService(IDatabase database, DiscordShardedClient client)
        {
            Database = database;
            Client = client;
            SetAgent();
            Client.MessageReceived += MessageReceived;
        }

        public void SetAgent()
        {
            var config = Database.Load<ConversationConfig>(ConversationConfig.DocumentName());
            if (config == null)
            {
                Agent = null;
                Config = null;
                return;
            }

            Config = config;
            var credentials = GoogleCredential.FromJson(config.ApiJson);
            var channel = new Grpc.Core.Channel(SessionsClient.DefaultEndpoint.Host, credentials.ToChannelCredentials());
            Agent = SessionsClient.Create(channel);
        }

        public ConversationConfig Config {get;set;}

        public SessionsClient Agent {get;set;}

        public IDatabase Database { get; }
        public DiscordShardedClient Client { get; }

        public bool IsEnabled()
        {
            return Agent != null && Config != null;
        }

        public async Task MessageReceived(SocketMessage msg)
        {
            try
            {
                if (!IsEnabled())
                {
                    return;
                }

                if (!(msg is SocketUserMessage message))
                {
                    return;
                }

                if (message.Author.IsBot || message.Author.IsWebhook)
                {
                    return;
                }

                int argPos = 0;
                if (!message.HasMentionPrefix(Client.CurrentUser, ref argPos))
                {
                    return;
                }

                var messageContent = message.Content;
                foreach (var usermention in message.MentionedUsers)
                {
                    messageContent = messageContent.Replace(usermention.Mention, usermention.Username);
                }
                foreach (var rolemention in message.MentionedRoles)
                {
                    messageContent = messageContent.Replace(rolemention.Mention, rolemention.Name);
                }
                foreach (var channelmention in message.MentionedChannels)
                {
                    messageContent = messageContent.Replace($"<#{channelmention.Id}>", channelmention.Name);
                }

                var query = new QueryInput
                {
                    Text = new TextInput
                    {
                        Text = message.Content.Substring(argPos),
                        LanguageCode = "en-us"
                    }
                };

                var session = new SessionName(Config.Certificate.project_id, $"{message.Author.Id}{message.Channel.Id}");
                var dialogResponse = Agent.DetectIntent(session, query);
                await message.Channel.SendMessageAsync(dialogResponse.QueryResult.FulfillmentText);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

    }
}