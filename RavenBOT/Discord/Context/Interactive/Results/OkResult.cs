using Discord.Commands;

namespace RavenBOT.Discord.Context.Interactive.Results
{
    public class OkResult : RuntimeResult
    {
        public OkResult(string reason = null) : base(null, reason)
        {
        }
    }
}