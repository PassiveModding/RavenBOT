using System;
using System.Threading.Tasks;
using Discord;
using Google.Cloud.Translation.V2;
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
                    guildConfig.Use(inputText.Length, $"Translated to {languageCode}: {inputText}");
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