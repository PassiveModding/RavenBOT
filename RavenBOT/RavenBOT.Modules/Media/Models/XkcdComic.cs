using Newtonsoft.Json;

namespace RavenBOT.Modules.Media.Models
{
    /// <summary>
    /// A class for serializing xkcd api responses
    /// </summary>
    public class XkcdComic
    {
        public int Num { get; set; }

        public string Month { get; set; }

        public string Year { get; set; }

        [JsonProperty("safe_title")]
        public string Title { get; set; }

        [JsonProperty("img")]
        public string ImageLink { get; set; }

        public string Alt { get; set; }
    }
}