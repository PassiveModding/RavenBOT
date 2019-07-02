using System;
using System.Collections.Generic;

namespace RavenBOT.ELO.Modules.Models
{
    public class Player
    {
        public static string DocumentName(ulong guildId, ulong userId)
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

        public Player(ulong userId, ulong guildId, string displayName)
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
        public Dictionary<string, int> AdditionalProperties { get; set; } = new Dictionary<string, int>();

  
        public void UpdateValue(string key, ModifyState state, int modifier)
        {
            if (AdditionalProperties.TryGetValue(key, out int value))
            {
                AdditionalProperties[key] = ModifyValue(state, value, modifier);
            }
            else
            {
                AdditionalProperties.Add(key, ModifyValue(state, 0, modifier));
            }
        }

        
        public enum ModifyState
        {
            Add,
            Subtract,
            Set
        }

        public static int ModifyValue(ModifyState state, int currentAmount, int modifyAmount)
        {
            switch (state)
            {
                case ModifyState.Add:
                    return currentAmount + modifyAmount;
                case ModifyState.Subtract:
                    return currentAmount - Math.Abs(modifyAmount);
                case ModifyState.Set:
                    return modifyAmount;
                default:
                    throw new ArgumentException("Provided modifystate is not valid.");
            }
        }
    }
}