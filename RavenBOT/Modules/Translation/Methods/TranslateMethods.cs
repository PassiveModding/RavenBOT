using System;
using System.Threading.Tasks;
using Discord;
using Google.Cloud.Translation.V2;
using RavenBOT.Extensions;
using static RavenBOT.Modules.Translation.Models.LanguageMap;

namespace RavenBOT.Modules.Translation.Methods
{
    public partial class TranslateService
    {
        public static string LanguageCodeToString(LanguageCode? code)
        {
            if (code == null)
            {
                return null;
            }

            var language = code.ToString();
            if (language == "zh_CN")
            {
                language = "zh-CN";
            }

            if (language == "zh_TW")
            {
                language = "zh-TW";
            }

            if (language == "_is")
            {
                language = "is";
            }

            return language;
        }

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

            public Result ResponseResult {get;set;}
            public TranslationResult TranslateResult {get;set;}

            public int RemainingUses {get;set;} = 0;
        }

        public EmbedBuilder TranslateEmbed(ulong guildId, IEmbed embed, LanguageCode code)
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

            return builder;
        }

        public TranslateResponse Translate(ulong guildId, string inputText, LanguageCode languageCode)
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
                var response = TranslateText(inputText, LanguageCodeToString(languageCode));
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

            return new TranslateResponse(TranslateResponse.Result.TranslationError); ;
        }

        private TranslationResult TranslateText(string inputText, string languageCode)
        {
            if (TranslationClient == null)
            {
                return null;
            }

            var response = TranslationClient.TranslateText(inputText, languageCode);
            
            return response;
        }
    }
}