using RavenBOT.Handlers;

namespace RavenBOT.Models
{
    public class GuildModel
    {
        /// <summary>
        ///     The Server ID
        /// </summary>
        public ulong ID { get; set; }

        public GuildSettings Settings { get; set; } = new GuildSettings();

        public class GuildSettings
        {
            public string CustomPrefix { get; set; } = null;
        }

        public void Save()
        {
            using (var Session = DatabaseHandler.Store.OpenSession())
            {
                Session.Store(this, ID.ToString());
                Session.SaveChanges();
            }
        }
    }
}
