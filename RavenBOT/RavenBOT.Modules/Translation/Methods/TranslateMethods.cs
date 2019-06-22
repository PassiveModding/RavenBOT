using System;
using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using Google.Cloud.Translation.V2;
using RavenBOT.Extensions;
using RavenBOT.Modules.Translation.Models;

namespace RavenBOT.Modules.Translation.Methods
{
    public partial class TranslateService
    {
        public string TranslateType = "Translate";

        public class TranslateResponse
        {
            public TranslateResponse(Result responseResult, TranslationResult translateResult = null, int? remainingUses = null)
            {
                ResponseResult = responseResult;
                TranslateResult = translateResult;
                RemainingUses = remainingUses ?? 0;
            }

            public enum Result
            {
                Success,
                InvalidInputText,
                InsufficientBalance,
                TranslationClientNotEnabled,
                TranslationError
            }

            public Result ResponseResult { get; set; }
            public TranslationResult TranslateResult { get; set; }

            public class TranslationResult 
            {
                public TranslateConfig.ApiKey ApiType { get; set; }
                public string SourceLanguage { get; set; }

                public string DestinationLanguage { get; set; }

                public string SourceText { get; set; }

                public string TranslatedText { get; set; }
            }

            public int RemainingUses { get; set; } = 0;
        }

        public EmbedBuilder TranslateEmbed(ulong guildId, IEmbed embed, string code)
        {
            if (embed.Type != EmbedType.Rich)
            {
                return null;
            }

            var builder = new EmbedBuilder()
            {
                Timestamp = embed.Timestamp,
                Color = embed.Color
            };

            if (!string.IsNullOrWhiteSpace(embed.Title))
            {
                var titleResult = Translate(guildId, embed.Title, code);
                if (titleResult.ResponseResult == TranslateResponse.Result.Success)
                {
                    builder.Title = titleResult.TranslateResult.TranslatedText.FixLength(100);
                }
            }

            if (embed.Author.HasValue)
            {
                //There should be no need to translate the author field.
                builder.Author = new EmbedAuthorBuilder()
                {
                    IconUrl = embed.Author.Value.IconUrl,
                    Name = embed.Author.Value.Name,
                    Url = embed.Author.Value.Url
                };
            }

            if (embed.Footer.HasValue)
            {
                if (!string.IsNullOrWhiteSpace(embed.Footer.Value.Text))
                {
                    var footerTextResult = Translate(guildId, embed.Footer.Value.Text, code);
                    if (footerTextResult.ResponseResult == TranslateResponse.Result.Success)
                    {
                        builder.Footer = new EmbedFooterBuilder()
                        {
                        IconUrl = embed.Footer.Value.IconUrl,
                        Text = footerTextResult.TranslateResult.TranslatedText.FixLength(250)
                        };
                    }
                }
                else
                {
                    builder.Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = embed.Footer.Value.IconUrl
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(embed.Description))
            {
                var description = Translate(guildId, embed.Description, code);
                if (description.ResponseResult == TranslateResponse.Result.Success)
                {
                    builder.Description = description.TranslateResult.TranslatedText.FixLength(2047);
                }
            }

            foreach (var field in embed.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.Name))
                {
                    continue;
                }

                var nameResult = Translate(guildId, field.Name, code);
                if (nameResult.ResponseResult != TranslateResponse.Result.Success || string.IsNullOrWhiteSpace(field.Value))
                {
                    continue;
                }

                var contentResult = Translate(guildId, field.Value, code);
                if (contentResult.ResponseResult != TranslateResponse.Result.Success)
                {
                    continue;
                }

                var newField = new EmbedFieldBuilder()
                {
                    Name = nameResult.TranslateResult.TranslatedText,
                    Value = contentResult.TranslateResult.TranslatedText,
                    IsInline = field.Inline
                };
                builder.Fields.Add(newField);
            }

            if (Config.ApiKeyType == TranslateConfig.ApiKey.Yandex)
            {
                if (builder.Fields.Count < 25 && builder.Length < 5900)
                {
                    builder.AddField("Yandex", "[Powered by Yandex](http://translate.yandex.com/)");
                }                
            }


            return builder;
        }

        public TranslateResponse Translate(ulong guildId, string inputText, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                return new TranslateResponse(TranslateResponse.Result.InvalidInputText);
            }

            var guildConfig = License.GetQuantifiableUser(TranslateType, guildId);
            if (guildConfig.RemainingUses() < inputText.Length)
            {
                //Do not translate if they do not have enough remaining translate uses.
                return new TranslateResponse(TranslateResponse.Result.InsufficientBalance);
            }

            try
            {
                var response = TranslateText(inputText, languageCode);
                if (response != null)
                {
                    guildConfig.UseNoLog(inputText.Length);
                    License.SaveUser(guildConfig);
                    return new TranslateResponse(TranslateResponse.Result.Success, response, guildConfig.RemainingUses());
                }
                return new TranslateResponse(TranslateResponse.Result.TranslationClientNotEnabled);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new TranslateResponse(TranslateResponse.Result.TranslationError);;
        }

        private TranslateResponse.TranslationResult TranslateText(string inputText, string languageCode)
        {
            try
            {
                var result = Translator.TranslateText(inputText, languageCode);
                if (result != null)
                {
                    return result;
                } 

                return null;               
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString(), LogSeverity.Error);
                return null;
            }
        }

        public static string FixTranslatedString(string value)
        {            
            var translationString = value;

            try
            {
                //Used to fix links that are created using the []() firmat.
                translationString = translationString.Replace("] (", "](");

                //Fixed user mentions
                var matchUser = Regex.Matches(translationString, @"(<@!?) (\d+)>");
                if (matchUser.Any())
                {
                    foreach (Match match in matchUser)
                    {
                        translationString = translationString.Replace(match.Value, $"{match.Groups[1].Value}{match.Groups[2].Value}>");
                    }
                }

                //fixed role mentions
                var matchRole = Regex.Matches(translationString, @"<@ & (\d+)>");
                if (matchRole.Any())
                {
                    foreach (Match match in matchRole)
                    {
                        translationString = translationString.Replace(match.Value, $"<@&{match.Groups[1].Value}>");
                    }
                }

                //Fixed channel mentions
                var matchChannel = Regex.Matches(translationString, @"<# (\d+)>");
                if (matchChannel.Any())
                {
                    foreach (Match match in matchChannel)
                    {
                        translationString = translationString.Replace(match.Value, $"<#{match.Groups[1].Value}>");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return translationString;
        }
    }
}