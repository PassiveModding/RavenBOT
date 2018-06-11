namespace RavenBOT.Models
{
    /// <summary>
    /// The config model.
    /// </summary>
    public class ConfigModel
    {
        /// <summary>
        /// Gets or sets the amount of shards for the bot
        /// </summary>
        public int Shards { get; set; } = 1;
        
        /// <summary>
        /// Gets or sets the bot prefix
        /// </summary>
        public string Prefix { get; set; } = "+";

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        public string Token { get; set; } = "Token";

        /// <summary>
        /// Gets or sets a value indicating whether to log user messages.
        /// </summary>
        public bool LogUserMessages { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to log command usages.
        /// </summary>
        public bool LogCommandUsages { get; set; } = true;
    }
}