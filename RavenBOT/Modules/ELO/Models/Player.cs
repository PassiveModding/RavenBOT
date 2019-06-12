using System.Collections.Generic;

namespace RavenBOT.Modules.ELO.Models
{
    public class Player
    {
        public static string DocumentName (ulong guildId, ulong userId)
        {
            return $"Player-{guildId}-{userId}";
        }

        /// <summary>
        /// The user display name
        /// </summary>
        /// <value></value>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// The user ID
        /// </summary>
        /// <value></value>
        public ulong UserId { get; set; }

        /// <summary>
        /// The server ID
        /// </summary>
        /// <value></value>
        public ulong GuildId { get; set; }

        public Player (ulong userId, ulong guildId, string displayName)
        {
            this.DisplayName = displayName;
            this.UserId = userId;
            this.GuildId = guildId;
        }

        /// <summary>
        /// Indicates the user's points.
        /// This is the primary value used to rank users.
        /// </summary>
        /// <value></value>
        public int Points { get; set; } = 0;

        /// <summary>
        /// A set of additional integer values that can be defined in the current server.
        /// </summary>
        /// <typeparam name="string">The property name</typeparam>
        /// <typeparam name="int">The value</typeparam>
        /// <returns></returns>
        public Dictionary<string, int> AdditionalProperties { get; set; } = new Dictionary<string, int> ();

        public void SetValue (string key, int count)
        {
            AdditionalProperties[key] = count;
        }

        public void UpdateValue (string key, int modifier)
        {
            if (AdditionalProperties.TryGetValue (key, out int value))
            {
                AdditionalProperties[key] = value + modifier;
            }
            else
            {
                AdditionalProperties.Add (key, modifier);
            }
        }
    }
}