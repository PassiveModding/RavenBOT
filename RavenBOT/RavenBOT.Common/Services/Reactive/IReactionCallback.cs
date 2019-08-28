using System;
using System.Threading.Tasks;

using Discord.Commands;
using Discord.WebSocket;

namespace RavenBOT.Common.Reactive
{
    /// <summary>
    /// Defines the functionality of a message using reaction callbacks.
    /// </summary>
    public interface IReactionCallback
    {
        RunMode RunMode { get; }

        TimeSpan? Timeout { get; }

        SocketCommandContext Context { get; }

        Task<bool> HandleCallbackAsync(SocketReaction reaction);
    }
}