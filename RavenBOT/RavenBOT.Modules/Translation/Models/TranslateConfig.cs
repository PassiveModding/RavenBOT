namespace RavenBOT.Modules.Translation.Models
{
    public class TranslateConfig
    {
        public static string DocumentName()
        {
            return $"TranslateConfig";
        }

        public string APIKey { get; set; }
        public bool Enabled { get; set; } = false;
        public string StoreUrl { get; set; }

        public ApiKey ApiKeyType { get; set; } = ApiKey.Google;

        public enum ApiKey
        {
            Google,
            Yandex
        }
    }
}