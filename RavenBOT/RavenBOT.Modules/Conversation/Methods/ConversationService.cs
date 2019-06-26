using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Dialogflow.V2;
using Grpc.Auth;
using RavenBOT.Common;
using RavenBOT.Common.Handlers;
using RavenBOT.Common.Interfaces;
using RavenBOT.Common.Services;
using RavenBOT.Modules.Conversation.Models;

namespace RavenBOT.Modules.Conversation.Methods
{
    public class ConversationService : IServiceable
    {
        public ConversationService(IDatabase database, DiscordShardedClient client, LogHandler logger, LocalManagementService localManagementService)
        {
            Database = database;
            Client = client;
            Logger = logger;
            LocalManagementService = localManagementService;
            SetAgent();
            Client.MessageReceived += MessageReceived;
            ConversationFunctions = new ConversationFunctions();
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

        public ConversationConfig Config { get; set; }

        public SessionsClient Agent { get; set; }

        public IDatabase Database { get; }
        public DiscordShardedClient Client { get; }
        public LogHandler Logger { get; }
        public LocalManagementService LocalManagementService { get; }
        public ConversationFunctions ConversationFunctions { get; }

        public bool IsEnabled()
        {
            return Agent != null && Config != null;
        }

        public async Task MessageReceived(SocketMessage msg)
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

            if (message.Channel is SocketTextChannel tChannel)
            {
                if (tChannel.Guild == null)
                {
                    return;
                }

                if (!LocalManagementService.LastConfig.IsAcceptable(tChannel.Guild.Id))
                {
                    return;
                }
            }

            var messageContent = message.Content;
            foreach (var usermention in message.MentionedUsers)
            {
                messageContent = messageContent.Replace($"<@{usermention.Id}>", usermention.Username);
                messageContent = messageContent.Replace($"<@!{usermention.Id}>", usermention.Username);
            }
            foreach (var rolemention in message.MentionedRoles)
            {
                messageContent = messageContent.Replace(rolemention.Mention, rolemention.Name);
            }
            foreach (var channelmention in message.MentionedChannels)
            {
                messageContent = messageContent.Replace($"<#{channelmention.Id}>", channelmention.Name);
            }

            messageContent = message.Content.Substring(argPos);

            var query = new QueryInput
            {
                Text = new TextInput
                {
                Text = messageContent,
                LanguageCode = "en-us"
                }
            };

            var session = new SessionName(Config.Certificate.project_id, $"{message.Author.Id}{message.Channel.Id}");
            var dialogResponse = Agent.DetectIntent(session, query);
            if (!string.IsNullOrWhiteSpace(dialogResponse.QueryResult.FulfillmentText))
            {
                /*
                //Ensure all parameters have been set before sending data
                if (!dialogResponse.QueryResult.Parameters.Fields.Any(x => string.IsNullOrWhiteSpace(x.Value.StringValue)))
                {
                    //If the response has a display name that is the same as one of the functions defined in conversationfunctions
                    //Run that function with the fulfillment test as a parameter
                    if (ConversationFunctions.GetFunctions().Contains(dialogResponse.QueryResult.Intent.DisplayName))
                    {
                        ConversationFunctions.ConversationResponse response = null;
                        //NOTE: If the fulfillment text is json the external braces must be doubled.
                        if (ConversationFunctions.TryInvoke(dialogResponse.QueryResult.Intent.DisplayName, ref response, dialogResponse.QueryResult.FulfillmentText))
                        {
                            if (response == null)
                            {
                                return;
                            }
                            await message.Channel.SendMessageAsync(response.Value);
                            Logger.Log($"Handled Rich Conversation, IN: {messageContent} => OUT: {dialogResponse.QueryResult.FulfillmentText}");  

                            //Return to discard regular response types
                            return;
                        }
                    }
                }
                */

                await message.Channel.SendMessageAsync(dialogResponse.QueryResult.FulfillmentText);
                Logger.Log($"Handled Conversation, IN: {messageContent} => OUT: {dialogResponse.QueryResult.FulfillmentText}");
            }

        }

    }
}