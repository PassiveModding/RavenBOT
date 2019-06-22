using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace RavenBOT.Common.Attributes
{
    //[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]

    public abstract class PreconditionBase : PreconditionAttribute
    {
        public abstract string PreviewText();
        public abstract string Name(); 
    }
}