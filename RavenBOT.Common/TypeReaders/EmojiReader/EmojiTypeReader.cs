using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace RavenBOT.Common.TypeReaders
{
    public class EmojiTypeReader : TypeReader
    {
        /// <summary>
        ///     tries to parse an emoji as a parameter
        /// </summary>
        /// <param name="context">
        ///     The context.
        /// </param>
        /// <param name="input">
        ///     The input.
        /// </param>
        /// <param name="services">
        ///     The services.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            try
            {
                var result = EmojiExtensions.FromText(input);
                return Task.FromResult(TypeReaderResult.FromSuccess(result));
            }
            catch
            {
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input could not be parsed as a Emoji."));
            }
        }
    }
}