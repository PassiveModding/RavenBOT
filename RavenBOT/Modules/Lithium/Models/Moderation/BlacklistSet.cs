using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RavenBOT.Modules.Lithium.Models.Moderation
{
    public class BlacklistSet
    {
        public string Response { get; set; }

        public Tuple<bool, string, bool> CheckMatch(string input)
        {
            var match = Messages.FirstOrDefault(x =>
            {
                if (x.Regex)
                {
                    return Regex.IsMatch(input, x.Content, RegexOptions.IgnoreCase);
                }

                return input.Contains(x.Content, StringComparison.InvariantCultureIgnoreCase);
            });

            if (match == null)
            {
                return new Tuple<bool, string, bool>(false, null, false);
            }

            return new Tuple<bool, string, bool>(true, match.Content, match.Regex);
        }

        public List<BlacklistMessage> Messages { get; set; } = new List<BlacklistMessage>();

        public class BlacklistMessage
        {
            public string Content { get; set; }
            public bool Regex { get; set; }
        }
    }
}