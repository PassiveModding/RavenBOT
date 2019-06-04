namespace RavenBOT.Modules.Statistics.Models
{
    public class DBLApiConfig
    {
        public static string DocumentName()
        {
            return "DBLConfig";
        }

        public string APIKey {get;set;} = null;
    }
}