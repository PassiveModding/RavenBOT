namespace RavenBOT.Modules.Statistics.Models
{
    public class GraphiteConfig
    {
        public static string DocumentName()
        {
            return $"GraphiteConfig";
        }
        public string GraphiteUrl { get; set; }
    }
}