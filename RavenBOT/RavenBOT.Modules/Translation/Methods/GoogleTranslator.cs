using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Google.Cloud.Translation.V2;

namespace RavenBOT.Modules.Translation.Methods
{
    public class GoogleTranslator : ITranslator
    {
        public TranslationClient TranslationClient { get; }
        //public string ApiKey { get; }
        public GoogleTranslator(string apiKey)
        {
            //ApiKey = apiKey;
            TranslationClient = TranslationClient.CreateFromApiKey(apiKey);
            PopulateLanguages();
            AvailableLanguages = GetAvailableLanguages();
        }

        public SpecificCulture[] AvailableLanguages { get; set; }

        public bool IsValidLanguageCode(string code)
        {
            if (AvailableLanguages.All(x => !x.BaseCulture.Name.Equals(code, StringComparison.InvariantCultureIgnoreCase) && !x.SpecificName.Equals(code, StringComparison.InvariantCultureIgnoreCase)))
            {
                return false;
            }

            return true;
        }

        public SpecificCulture[] GetAvailableLanguages()
        {
            return AvailableLanguages;
        }

        private void PopulateLanguages()
        {
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures).Select(x => new SpecificCulture(x));
            var gCultureResponse = TranslationClient.ListLanguages();

            AvailableLanguages = cultures
                .Where(c => gCultureResponse.Any(g => g.Code.Equals(c.BaseCulture.Name, StringComparison.InvariantCultureIgnoreCase) || g.Code.Equals(c.SpecificName, StringComparison.InvariantCultureIgnoreCase)))
                .ToArray();
        }

        public TranslateService.TranslateResponse.TranslationResult TranslateText(string source, string targetLanguage)
        {
            if (TranslationClient == null)
            {
                return null;
            }

            if (!IsValidLanguageCode(targetLanguage))
            {
                throw new Exception("Invalid Target Language Code.");
            }

            var response = TranslationClient.TranslateText(source, targetLanguage);
            var result = new TranslateService.TranslateResponse.TranslationResult
            {
                SourceLanguage = response.DetectedSourceLanguage,
                SourceText = source,
                DestinationLanguage = response.TargetLanguage,
                TranslatedText = TranslateService.FixTranslatedString(response.TranslatedText)
            };
            return result;
        }

        public TranslateService.TranslateResponse.TranslationResult TranslateText(string source, string sourceLanguage, string targetLanguage)
        {
            if (TranslationClient == null)
            {
                return null;
            }

            if (!IsValidLanguageCode(targetLanguage))
            {
                throw new Exception("Invalid Target Language Code.");
            }
            else if (!IsValidLanguageCode(sourceLanguage))
            {
                throw new Exception("Invalid Source Language Code.");
            }

            var response = TranslationClient.TranslateText(source, targetLanguage, sourceLanguage);
            var result = new TranslateService.TranslateResponse.TranslationResult
            {
                SourceLanguage = response.DetectedSourceLanguage,
                SourceText = source,
                DestinationLanguage = response.TargetLanguage,
                TranslatedText = TranslateService.FixTranslatedString(response.TranslatedText)
            };
            return result;
        }
    }
}