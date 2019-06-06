using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Commands.Builders;

namespace RavenBOT.Modules.Tickets
{
    /// <summary>
    /// Interactive base extension which implements module prefixes without the additional space delimiter.
    /// Set the group property in the ctor.
    /// Do not use a group attribute, they will concatenate.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class GroupedBase<T> : InteractiveBase<T> where T : ShardedCommandContext
    { 
        protected string Group {get; set;}

        protected override void OnModuleBuilding(CommandService commandService, Discord.Commands.Builders.ModuleBuilder builder)
        {
            var type = typeof(CommandBuilder);
            var list = type.GetField("_aliases", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var command in builder.Commands)
            {
                var aliases = (List<string>)list.GetValue(command);
                for (int i = 0; i < aliases.Count; i++)
                {
                    aliases[i] = $"{Group}{aliases[i]}";
                }
            }      
        }        
    }
}