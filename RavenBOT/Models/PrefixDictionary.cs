namespace RavenBOT.Models
{
    using System.Collections.Generic;

    using RavenBOT.Handlers;

    public class PrefixDictionary
    {
        /// <summary>
        /// Gets or sets the prefix list.
        /// The format is GuildId, Prefix
        /// </summary>
        public Dictionary<ulong, string> PrefixList { get; set; } = new Dictionary<ulong, string>();

        /// <summary>
        /// The guild prefix.
        /// </summary>
        /// <param name="guildId">
        /// The guild id.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// This will be null if the guild does not have a set prefix
        /// </returns>
        public string GuildPrefix(ulong guildId)
        {
            PrefixList.TryGetValue(guildId, out var prefix);
            return prefix;
        }

        /// <summary>
        /// Saves the GuildModel
        /// </summary>
        /// <returns>
        /// The <see cref="PrefixDictionary"/>.
        /// </returns>
        public static PrefixDictionary Load()
        {
            using (var session = DatabaseHandler.Store.OpenSession())
            {
                var list = session.Load<PrefixDictionary>("PrefixList") ?? new PrefixDictionary();

                session.Dispose();
                return list;
            }
        }

        /// <summary>
        /// Saves the GuildModel
        /// </summary>
        public void Save()
        {
            using (var session = DatabaseHandler.Store.OpenSession())
            {
                session.Store(this, "PrefixList");
                session.SaveChanges();
            }
        }
    }
}
