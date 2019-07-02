using System.Collections.Generic;
using System.Globalization;
namespace RavenBOT.Modules.Translation.Methods
{
    public class SpecificCulture
    {
        public SpecificCulture(CultureInfo culture)
        {
            BaseCulture = culture;
            string specName = "(none)";
            try { specName = CultureInfo.CreateSpecificCulture(BaseCulture.Name).Name; }
            catch {}
            SpecificName = specName;
        }
        public CultureInfo BaseCulture { get; }
        public string SpecificName { get; }
    }

    public interface ITranslator
    {
        TranslateService.TranslateResponse.TranslationResult TranslateText(string source, string targetLanguage);
        TranslateService.TranslateResponse.TranslationResult TranslateText(string source, string sourceLanguage, string targetLanguage);

        SpecificCulture[] GetAvailableLanguages();

        bool IsValidLanguageCode(string code);
    }
}