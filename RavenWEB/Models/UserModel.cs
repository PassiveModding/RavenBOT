using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RavenWEB.Models
{
    public class UserModel
    {
        public string username { get; set; }
        public string locale { get; set; }
        public bool mfa_enabled { get; set; }
        public int flags { get; set; }
        public string avatar { get; set; }
        public string discriminator { get; set; }
        public string id { get; set; }
    }
}
