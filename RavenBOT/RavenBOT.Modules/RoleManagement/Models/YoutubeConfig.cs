namespace RavenBOT.Modules.RoleManagement.Models
{
    public class YoutubeConfig
    {
        public static string DocumentName() => "YoutubeConfig";

        public string ApiKey { get; set; }
    }
}