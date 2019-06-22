using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Google.Cloud.Translation.V2;
using RavenBOT.Common;
using RavenBOT.Common.Handlers;
using RavenBOT.Common.Interfaces;
using RavenBOT.Common.Services;
using RavenBOT.Extensions;
using RavenBOT.Modules.Translation.Models;

namespace RavenBOT.Modules.Translation.Methods
{
    public partial class TranslateService : IServiceable
    {
        public ITranslator Translator { get; }
        public IDatabase Database { get; }
        public LicenseService License { get; }
        public LogHandler Logger { get; }
        public DiscordShardedClient Client { get; }
        public LocalManagementService LocalManagementService { get; }
        public TranslateConfig Config { get; }

        public TranslateService(IDatabase database, LicenseService license, LogHandler logger, DiscordShardedClient client, LocalManagementService localManagementService)
        {
            Database = database;
            License = license;
            Logger = logger;
            Client = client;
            LocalManagementService = localManagementService;
            Config = GetTranslateConfig();
            if (Config.APIKey != null && Config.Enabled)
            {
                if (Config.ApiKeyType == TranslateConfig.ApiKey.Google)
                {
                    Translator = new GoogleTranslator(Config.APIKey);
                }
                else if (Config.ApiKeyType == TranslateConfig.ApiKey.Yandex)
                {
                    Translator = new YandexTranslator(Config.APIKey);
                }
                else
                {
                    throw new NotImplementedException("The specified api type is not implemented");
                }

                Client.ReactionAdded += ReactionAdded;
            }
        }

        //Contains the message IDs of translated messages.
        private readonly Dictionary<ulong, List<string>> Translated = new Dictionary<ulong, List<string>>();

        public LanguageMap.TranslationSet GetCode(TranslateGuild config, SocketReaction reaction)
        {
            var languageType = config.CustomPairs.FirstOrDefault(x => x.EmoteMatches.Any(val => val == reaction.Emote.Name));

            if (languageType == null)
            {
                languageType = LanguageMap.DefaultMap.FirstOrDefault(x => x.EmoteMatches.Any(val => val == reaction.Emote.Name));
                if (languageType == null)
                {
                    return null;
                }
            }

            return languageType;
        }

        public async Task ReactionAdded(Cacheable<IUserMessage, ulong> messageCacheable, ISocketMessageChannel mChannel, SocketReaction reaction)
        {
            if (!(mChannel is ITextChannel channel) || !reaction.User.IsSpecified)
            {
                return;
            }

            if (!LocalManagementService.LastConfig.IsAcceptable(channel.GuildId))
            {
                return;
            }

            var message = await TryGetMessage(messageCacheable, channel, reaction);
            if (message == null)
            {
                return;
            }
            else if (string.IsNullOrWhiteSpace(message.Content) && message.Embeds.All(x => x.Type != EmbedType.Rich))
            {
                return;
            }

            if (reaction.User.Value.IsBot || reaction.User.Value.IsWebhook)
            {
                return;
            }

            var config = GetTranslateGuild(channel.GuildId);
            if (!config.ReactionTranslations)
            {
                return;
            }

            //Ensure whitelist isn't enforced unless the list is populated
            if (config.WhitelistRoles.Any())
            {
                //Check to see if the user has a whitelisted role
                if (!config.WhitelistRoles.Any(x => (message.Author as IGuildUser)?.RoleIds.Contains(x) == true))
                {
                    return;
                }
            }

            var languageType = GetCode(config, reaction);
            if (languageType == null)
            {
                return;
            }

            if (Translated.ContainsKey(message.Id) && Translated[message.Id].Any(x => x.Equals(languageType.LanguageString, StringComparison.InvariantCultureIgnoreCase)))
            {
                return;
            }

            var response = Translate(channel.GuildId, message.Content, languageType.LanguageString);

            var embed = message.Embeds.FirstOrDefault(x => x.Type == EmbedType.Rich);
            EmbedBuilder translatedEmbed = null;
            if (embed != null)
            {
                var embedResponse = TranslateEmbed(channel.GuildId, embed, languageType.LanguageString);
                translatedEmbed = embedResponse;
            }

            if (response.ResponseResult != TranslateResponse.Result.Success && translatedEmbed == null)
            {
                return;
            }

            if (config.DirectMessageTranslations)
            {
                var user = reaction.User.Value;
                var dmChannel = await user.GetOrCreateDMChannelAsync();
                if (translatedEmbed != null)
                {
                    await dmChannel.SendMessageAsync(response?.TranslateResult?.TranslatedText ?? "", false, translatedEmbed?.Build()).ConfigureAwait(false);
                    Logger.Log($"Translated Embed to {languageType.LanguageString}");
                }
                else
                {
                    await dmChannel.SendMessageAsync("", false, GetTranslationEmbed(response).Build()).ConfigureAwait(false);
                    Logger.Log($"**Translated {response.TranslateResult.SourceLanguage}=>{response.TranslateResult.DestinationLanguage}**\n{response?.TranslateResult?.SourceText} \nto\n {response?.TranslateResult?.TranslatedText}");
                }
            }
            else
            {
                if (Translated.ContainsKey(message.Id))
                {
                    Translated[message.Id].Add(languageType.LanguageString);
                }
                else
                {
                    Translated.Add(message.Id, new List<string>() { languageType.LanguageString });
                }

                if (translatedEmbed != null)
                {
                    await channel.SendMessageAsync(response?.TranslateResult?.TranslatedText ?? "", false, translatedEmbed?.Build()).ConfigureAwait(false);
                    Logger.Log($"Translated Embed to {languageType.LanguageString}");
                }
                else
                {
                    await channel.SendMessageAsync("", false, GetTranslationEmbed(response).Build()).ConfigureAwait(false);
                    Logger.Log($"**Translated {response.TranslateResult.SourceLanguage}=>{response.TranslateResult.DestinationLanguage}**\n{response?.TranslateResult?.SourceText} \nto\n {response?.TranslateResult?.TranslatedText}");
                }
            }

        }

        public EmbedBuilder GetTranslationEmbed(TranslateResponse result)
        {
            if (result.TranslateResult == null)
            {
                return null;
            }

            var embed = new EmbedBuilder();
            embed.AddField($"Original Message [{result.TranslateResult.SourceLanguage}]", result.TranslateResult.SourceText.FixLength());
            embed.AddField($"Translated Message [{result.TranslateResult.DestinationLanguage}]", result.TranslateResult.TranslatedText.FixLength());

            if (Config.ApiKeyType == TranslateConfig.ApiKey.Yandex)
            {
                embed.AddField("Yandex", $"[Powered by Yandex](http://translate.yandex.com/)");
            }

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
            //This should work however there is a bug in d.net that is not fixed in the current d.net 2.1.1 implementation
            /*else if (reaction.Message.IsSpecified)
            {
                message = reaction.Message.Value;
            }*/
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
                config = new TranslateGuild(guildId);
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