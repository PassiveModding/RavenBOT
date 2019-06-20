using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using RavenBOT.Common;
using RedditSharp;

namespace RavenBOT.Modules.Media.Methods
{
    public class MediaHelper : IServiceable
    {
        public HttpClient Client { get; }
        public Reddit Reddit { get; }

        public MediaHelper()
        {
            Client = new HttpClient();
            Reddit = new Reddit();
        }
    }
}