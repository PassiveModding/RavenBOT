using System;
using System.Collections.Generic;

namespace RavenBOT.Modules.Media.Models
{
    public class UrbanDictionaryModel
    {
        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        public List<string> tags { get; set; }

        /// <summary>
        /// Gets or sets the result_type.
        /// </summary>
        public string result_type { get; set; }

        /// <summary>
        /// Gets or sets the list.
        /// </summary>
        public List<List> list { get; set; }

        /// <summary>
        /// Gets or sets the sounds.
        /// </summary>
        public List<string> sounds { get; set; }

        /// <summary>
        /// The list.
        /// </summary>
        public class List
        {
            /// <summary>
            /// Gets or sets the definition.
            /// </summary>
            public string definition { get; set; }

            /// <summary>
            /// Gets or sets the permalink.
            /// </summary>
            public string permalink { get; set; }

            /// <summary>
            /// Gets or sets the thumbs_up.
            /// </summary>
            public int thumbs_up { get; set; }

            /// <summary>
            /// Gets or sets the author.
            /// </summary>
            public string author { get; set; }

            /// <summary>
            /// Gets or sets the word.
            /// </summary>
            public string word { get; set; }

            /// <summary>
            /// Gets or sets the defid.
            /// </summary>
            public int defid { get; set; }

            /// <summary>
            /// Gets or sets the current_vote.
            /// </summary>
            public string current_vote { get; set; }

            /// <summary>
            /// Gets or sets the written_on.
            /// </summary>
            public DateTime written_on { get; set; }

            /// <summary>
            /// Gets or sets the example.
            /// </summary>
            public string example { get; set; }

            /// <summary>
            /// Gets or sets the thumbs_down.
            /// </summary>
            public int thumbs_down { get; set; }
        }
    }
}