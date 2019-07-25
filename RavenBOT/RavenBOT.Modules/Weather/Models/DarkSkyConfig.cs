namespace RavenBOT.Modules.Weather.Models
{
    public class DarkSkyConfig
    {
        public static string DocumentName() => "DarkSkyConfig";
        public string ApiKey { get; set; }
    }
}