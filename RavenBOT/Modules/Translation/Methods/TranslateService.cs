using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Google.Cloud.Translation.V2;
using RavenBOT.Extensions;
using RavenBOT.Handlers;
using RavenBOT.Modules.Translation.Models;
using RavenBOT.Services;
using RavenBOT.Services.Database;
using RavenBOT.Services.Licensing;

namespace RavenBOT.Modules.Translation.Methods
{
    public partial class TranslateService : IServiceable
    {
        public TranslationClient TranslationClient {get;}

        public IDatabase Database {get;}
        public LicenseService License { get; }
        public LogHandler Logger { get; }
        public DiscordShardedClient Client { get; }

        public TranslateService(IDatabase database, LicenseService license, LogHandler logger, DiscordShardedClient client)
        {
            Database = database;
            License = license;
            Logger = logger;
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
        private readonly Dictionary<ulong, List<LanguageMap.LanguageCode>> Translated = new Dictionary<ulong, List<LanguageMap.LanguageCode>>();

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

            var languageType = GetCode(config, reaction);
            if (languageType == null)
            {
                return;
            }

            if (Translated.ContainsKey(message.Id) && Translated[message.Id].Contains(languageType.Language))
            {
                return;
            }

            var response = Translate(channel.GuildId, message.Content, languageType.Language);

            var embed = message.Embeds.FirstOrDefault(x => x.Type == EmbedType.Rich);
            EmbedBuilder translatedEmbed = null;
            if (embed != null)
            {
                var embedResponse = TranslateEmbed(channel.GuildId, embed, languageType.Language);
                translatedEmbed = embedResponse;
            }

            if (response.ResponseResult != TranslateResponse.Result.Success && translatedEmbed == null)
            {
                return;
            }

            if (Translated.ContainsKey(message.Id))
            {
                Translated[message.Id].Add(languageType.Language);
            }
            else
            {
                Translated.Add(message.Id, new List<LanguageMap.LanguageCode>() {languageType.Language});
            }

            if (config.DirectMessageTranslations)
            {
                var user = message.Author;
                var dmChannel = await user.GetOrCreateDMChannelAsync();
                if (translatedEmbed != null)
                {
                    await dmChannel.SendMessageAsync(response?.TranslateResult?.TranslatedText ?? "", false, translatedEmbed?.Build()).ConfigureAwait(false);
                }
                else
                {
                    await dmChannel.SendMessageAsync("", false, GetTranslationEmbed(response).Build()).ConfigureAwait(false);
                }
            }
            else
            {
                if (translatedEmbed != null)
                {
                    await channel.SendMessageAsync(response?.TranslateResult?.TranslatedText ?? "", false, translatedEmbed?.Build()).ConfigureAwait(false);
                }
                else
                {
                    await channel.SendMessageAsync("", false, GetTranslationEmbed(response).Build()).ConfigureAwait(false);
                }
            }

            Logger.Log($"Translated {response.TranslateResult.DetectedSourceLanguage}=>{response.TranslateResult.TargetLanguage}\n{response?.TranslateResult?.OriginalText} \nto\n {response?.TranslateResult?.TranslatedText}");
    }   

        public EmbedBuilder GetTranslationEmbed(TranslateResponse result)
        {
            if (result.TranslateResult == null)
            {
                return null;
            }

            var translationString = result.TranslateResult.TranslatedText;
            
            try
            {
                var matchUser = Regex.Matches(translationString, @"(<@!?) (\d+)>");
                if (matchUser.Any())
                {
                    foreach(Match match in matchUser)
                    {
                        translationString = translationString.Replace(match.Value, $"{match.Groups[1].Value}{match.Groups[2].Value}>");
                    }
                }

                var matchRole = Regex.Matches(translationString, @"<@ & (\d+)>");
                if (matchRole.Any())
                {
                    foreach(Match match in matchRole)
                    {
                        translationString = translationString.Replace(match.Value, $"<@&{match.Groups[1].Value}>");
                    }
                }

                var matchChannel = Regex.Matches(translationString, @"<# (\d+)>");
                if (matchChannel.Any())
                {
                    foreach(Match match in matchChannel)
                    {
                        translationString = translationString.Replace(match.Value, $"<#{match.Groups[1].Value}>");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            var embed = new EmbedBuilder();
            embed.AddField($"Original Message [{result.TranslateResult.SpecifiedSourceLanguage ?? result.TranslateResult.DetectedSourceLanguage}]", result.TranslateResult.OriginalText.FixLength());
            embed.AddField($"Translated Message [{result.TranslateResult.TargetLanguage}]", translationString.FixLength());
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