using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RavenWEB.Models
{
    public class GuildModel
    {
        public bool owner { get; set; }
        public ulong permissions { get; set; }
        public string icon { get; set; }
        public string id { get; set; }
        public string name { get; set; }
    }
}
