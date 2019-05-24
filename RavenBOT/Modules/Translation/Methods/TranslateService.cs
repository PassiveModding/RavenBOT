using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Google.Cloud.Translation.V2;
using RavenBOT.Extensions;
using RavenBOT.Modules.Translation.Models;
using RavenBOT.Services.Database;
using RavenBOT.Services.Licensing;
using static RavenBOT.Modules.Translation.Models.LanguageMap;

namespace RavenBOT.Modules.Translation.Methods
{
    public partial class TranslateService
    {
        public TranslationClient TranslationClient {get;}

        public IDatabase Database {get;}
        public LicenseService License { get; }
        public DiscordShardedClient Client { get; }

        public TranslateService(IDatabase database, LicenseService license, DiscordShardedClient client)
        {
            Database = database;
            License = license;
            Client = client;
            
            var config = GetTranslateConfig();
            if (config.APIKey != null && config.Enabled)
            {
                //NOTE: Should throw if invalid key is provided
                TranslationClient = TranslationClient.CreateFromApiKey(config.APIKey);
            }

            Client.ReactionAdded += ReactionAdded;
        }

        //Contains the message IDs of translated messages.
        private readonly Dictionary<ulong, List<LanguageCode>> Translated = new Dictionary<ulong, List<LanguageCode>>();

        public async Task ReactionAdded(Cacheable<IUserMessage, ulong> messageCacheable, ISocketMessageChannel mChannel, SocketReaction reaction)
        {
            if (!(mChannel is ITextChannel channel) || !reaction.User.IsSpecified)
            {
                return;
            }

            var message = await TryGetMessage(messageCacheable, channel, reaction);
            if (message == null || string.IsNullOrWhiteSpace(message.Content))
            {
                return;
            }

            var config = GetTranslateGuild(channel.GuildId);
            if (!config.ReactionTranslations)
            {
                return;
            }

            var languageType = config.CustomPairs.FirstOrDefault(x => x.EmoteMatches.Any(val => val == reaction.Emote.Name));

            if (languageType == null)
            {
                languageType = LanguageMap.DefaultMap.FirstOrDefault(x => x.EmoteMatches.Any(val => val == reaction.Emote.Name));
                if (languageType == null)
                {
                    return;
                }
            }

            if (Translated.ContainsKey(message.Id) && Translated[message.Id].Contains(languageType.Language))
            {
                return;
            }

            var response = Translate(channel.GuildId, message.Content, languageType.Language);
            if (response.ResponseResult != TranslateResponse.Result.Success)
            {
                return;
            }

            if (Translated.ContainsKey(message.Id))
            {
                Translated[message.Id].Add(languageType.Language);
            }
            else
            {
                Translated.Add(message.Id, new List<LanguageCode>() {languageType.Language});
            }

            var translateEmbed = GetTranslationEmbed(response);
            if (translateEmbed == null)
            {
                return;
            }

            if (config.DirectMessageTranslations)
            {
                var user = message.Author;
                var dmChannel = await user.GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync("", false, translateEmbed.Build()).ConfigureAwait(false);
            }
            else
            {
                await channel.SendMessageAsync("", false, translateEmbed.Build()).ConfigureAwait(false);
            }
    }   

        public EmbedBuilder GetTranslationEmbed(TranslateResponse result)
        {
            if (result.TranslateResult == null)
            {
                return null;
            }
            var embed = new EmbedBuilder();
            embed.AddField($"Original Message [{result.TranslateResult.SpecifiedSourceLanguage ?? result.TranslateResult.DetectedSourceLanguage}]", result.TranslateResult.OriginalText.FixLength());
            embed.AddField($"Translated Message [{result.TranslateResult.TargetLanguage}]", result.TranslateResult.TranslatedText.FixLength());
            embed.Color = Color.Green;
            embed.Footer = new EmbedFooterBuilder
            {
                Text = $"{result.RemainingUses} Remaining Characters"
            };
            return embed;
        }

        public async Task<IUserMessage> TryGetMessage(Cacheable<IUserMessage, ulong> messageCacheable, ITextChannel channel, SocketReaction reaction)
        {
            IUserMessage message;
            if (messageCacheable.HasValue)
            {
                message = messageCacheable.Value;
            }
            else if (reaction.Message.IsSpecified)
            {
                message = reaction.Message.Value;
            }
            else
            {
                var iMessage = await channel.GetMessageAsync(messageCacheable.Id);
                if (iMessage is IUserMessage uMessage)
                {
                    message = uMessage;
                }
                else
                {
                    return null;
                }
            }

            return message;
        }

        public TranslateConfig GetTranslateConfig()
        {
            var config = Database.Load<TranslateConfig>(TranslateConfig.DocumentName());
            if (config == null)
            {
                config = new TranslateConfig();
                Database.Store(config, TranslateConfig.DocumentName());
            }

            return config;
        }

        public TranslateGuild GetTranslateGuild(ulong guildId)
        {
            var config = Database.Load<TranslateGuild>(TranslateGuild.DocumentName(guildId));
            if (config == null)
            {
                config = new TranslateGuild();
                Database.Store(config, TranslateGuild.DocumentName(guildId));
            }

            return config;
        }

        public void SaveTranslateConfig(TranslateConfig config)
        {
            Database.Store(config, TranslateConfig.DocumentName());
        }

        public void SaveTranslateGuild(TranslateGuild guild)
        {
            Database.Store(guild, TranslateGuild.DocumentName(guild.GuildId));
        }
    }
}