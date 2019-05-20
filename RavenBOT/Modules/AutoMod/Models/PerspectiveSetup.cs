namespace RavenBOT.Modules.AutoMod.Models
{
    public class PerspectiveSetup
    {
        public static string DocumentName()
        {
            return $"PerspectiveSetup";
        }
        public string PerspectiveToken { get; set; }
    }
}
