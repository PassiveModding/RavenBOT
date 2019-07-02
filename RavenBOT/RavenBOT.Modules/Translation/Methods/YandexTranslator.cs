using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace RavenBOT.Modules.Translation.Methods
{
    public class YandexTranslator : ITranslator
    {
        public YandexTranslator(string apiKey)
        {
            Client = new HttpClient();
            ApiKey = apiKey;
            //Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            PopulateLanguages();
            AvailableLanguages = GetAvailableLanguages();
        }

        public HttpClient Client { get; }
        public string ApiKey { get; }

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
            var response = Client.GetAsync($"https://translate.yandex.net/api/v1.5/tr.json/getLangs?key={ApiKey}&ui=en").Result;
            if (!response.IsSuccessStatusCode)
            {
                AvailableLanguages = new SpecificCulture[] {};
            }

            var jResponse = JToken.Parse(response.Content.ReadAsStringAsync().Result);
            //var directions = jResponse.Value<JArray>("dirs");
            var langs = jResponse.Value<JObject>("langs");
            var cultureInfos = new List<SpecificCulture>();
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures).Select(x => new SpecificCulture(x));
            foreach (var lang in langs)
            {
                var cultureMatch = cultures.FirstOrDefault(x => x.BaseCulture.Name.Equals(lang.Key, StringComparison.InvariantCultureIgnoreCase) || x.SpecificName.Equals(lang.Key, StringComparison.InvariantCultureIgnoreCase));
                if (cultureMatch != null)
                {
                    cultureInfos.Add(cultureMatch);
                }
            }

            AvailableLanguages = cultureInfos.ToArray();
        }

        public TranslateService.TranslateResponse.TranslationResult TranslateText(string source, string targetLanguage)
        {
            if (!IsValidLanguageCode(targetLanguage))
            {
                throw new Exception("Invalid Target Language Code.");
            }

            //TODO: fix source text for uri encoding.
            var response = Client.GetAsync($"https://translate.yandex.net/api/v1.5/tr.json/translate?key={ApiKey}&text={Uri.EscapeDataString(source)}&lang={targetLanguage}").Result;
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseJson = response.Content.ReadAsStringAsync().Result;
            var token = JToken.Parse(responseJson);

            var result = new TranslateService.TranslateResponse.TranslationResult();

            var lang = token.Value<JToken>("lang").ToString();
            var splitChar = lang.IndexOf("-");

            //TODO: Default if split char is not found.
            var sourceLang = lang.Substring(0, splitChar);
            var destLang = lang.Substring(splitChar + 1);
            var text = token.Value<JArray>("text").FirstOrDefault().ToString();
            result.ApiType = Models.TranslateConfig.ApiKey.Yandex;
            result.DestinationLanguage = destLang;
            result.SourceLanguage = sourceLang;
            result.SourceText = source;
            result.TranslatedText = TranslateService.FixTranslatedString(text);

            return result;
        }

        public TranslateService.TranslateResponse.TranslationResult TranslateText(string source, string sourceLanguage, string targetLanguage)
        {
            if (!IsValidLanguageCode(targetLanguage))
            {
                throw new Exception("Invalid Target Language Code.");
            }
            else if (!IsValidLanguageCode(sourceLanguage))
            {
                throw new Exception("Invalid Source Language Code.");
            }

            source = Uri.EscapeDataString(source);
            var response = Client.GetAsync($"https://translate.yandex.net/api/v1.5/tr.json/translate?key={ApiKey}&text={source}&lang={sourceLanguage}-{targetLanguage}").Result;
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseJson = response.Content.ReadAsStringAsync().Result;
            var token = JToken.Parse(responseJson);

            var result = new TranslateService.TranslateResponse.TranslationResult();

            var text = token.Value<JArray>("text").FirstOrDefault().ToString();
            result.ApiType = Models.TranslateConfig.ApiKey.Yandex;
            result.DestinationLanguage = targetLanguage;
            result.SourceLanguage = sourceLanguage;
            result.SourceText = source;
            result.TranslatedText = TranslateService.FixTranslatedString(text);

            return result;
        }
    }
}