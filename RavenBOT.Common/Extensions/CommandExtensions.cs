using Discord.Commands;
using System.Collections.Generic;
using System.Linq;

namespace RavenBOT.Common
{
    public static partial class Extensions
    {
        public static string ParameterUsage(this IEnumerable<ParameterInfo> parameters)
        {
            return string.Join(" ", parameters.Select(x => x.ParameterInformation()));
        }

        public static string ParameterInformation(this ParameterInfo parameter)
        {
            var initial = parameter.Name + (parameter.Summary == null ? "" : $"({parameter.Summary})");
            var isAttributed = false;
            if (parameter.IsOptional)
            {
                initial = $"[{initial} = {parameter.DefaultValue ?? "null"}]";
                isAttributed = true;
            }

            if (parameter.IsMultiple)
            {
                initial = $"|{initial}|";
                isAttributed = true;
            }

            if (parameter.IsRemainder)
            {
                initial = $"...{initial}";
                isAttributed = true;
            }

            if (!isAttributed)
            {
                initial = $"<{initial}>";
            }

            return initial;
        }
    }
}