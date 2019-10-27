using Discord.Commands;

namespace RavenBOT.Common
{
    //[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]

    public abstract class PreconditionBase : PreconditionAttribute
    {
        public abstract string PreviewText();
        public abstract string Name();
    }
}