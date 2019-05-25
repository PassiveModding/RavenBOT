using System;
using System.Collections.Generic;

namespace RavenBOT.Modules.Media.Models
{
    /// <summary>
    /// A class for serializing urban dictionary api responses to
    /// </summary>
    public class UrbanDictionaryModel
    {
        public List<string> tags { get; set; }

        public string result_type { get; set; }

        public List<List> list { get; set; }


        public List<string> sounds { get; set; }

        public class List
        {
            public string definition { get; set; }

            public string permalink { get; set; }

            public int thumbs_up { get; set; }

            public string author { get; set; }

            public string word { get; set; }

            public int defid { get; set; }

            public string current_vote { get; set; }

            public DateTime written_on { get; set; }

            public string example { get; set; }

            public int thumbs_down { get; set; }
        }
    }
}