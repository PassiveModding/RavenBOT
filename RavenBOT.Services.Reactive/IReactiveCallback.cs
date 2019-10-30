using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

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