using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace RavenBOT.Common
{
    /// <summary>
    /// Defines the functionality of a message using reaction callbacks.
    /// </summary>
    public interface IReactiveCallback
    {
        RunMode RunMode { get; }

        SocketCommandContext Context { get; }

        Task<bool> HandleCallbackAsync(SocketReaction reaction);
    }
}