using Newtonsoft.Json;

namespace RavenBOT.Modules.Media.Models
{
    public class XkcdComic
    {
        /// <summary>
        /// Gets or sets the num.
        /// </summary>
        public int Num { get; set; }

        /// <summary>
        /// Gets or sets the month.
        /// </summary>
        public string Month { get; set; }

        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        public string Year { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [JsonProperty("safe_title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the image link.
        /// </summary>
        [JsonProperty("img")]
        public string ImageLink { get; set; }

        /// <summary>
        /// Gets or sets the alt.
        /// </summary>
        public string Alt { get; set; }
    }
}