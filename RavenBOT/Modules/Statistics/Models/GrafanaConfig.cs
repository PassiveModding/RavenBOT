namespace RavenBOT.Modules.Statistics.Models
{
    public class GrafanaConfig
    {
        public static string DocumentName()
        {
            return $"GrafanaConfig";
        }

        public string GrafanaUrl {get;set;}
        public string ApiKey {get;set;}
    }
}