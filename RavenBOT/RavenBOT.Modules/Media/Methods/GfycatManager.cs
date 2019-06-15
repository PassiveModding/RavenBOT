using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RavenBOT.Extensions;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Media.Methods
{
    public class GfycatManager : IServiceable
    {
        public IDatabase Database { get; }
        public HttpClient Client { get; }

        public GfycatManager(IDatabase database, HttpClient client)
        {
            Database = database;
            Client = client;
        }

        public class GfyCatOauthResponse
        {
            public string token_type { get; set; }
            public string scope { get; set; }
            public int expires_in { get; set; }
            public string access_token { get; set; }
            public DateTime CreationTime { get; set; } = DateTime.UtcNow;

            public DateTime ExpiresAt()
            {
                return CreationTime + TimeSpan.FromSeconds(expires_in);
            }

            public bool IsExpired()
            {
                return ExpiresAt() <= DateTime.UtcNow;
            }
        }

        public class GfycatClientInfo
        {
            public string client_id { get; set; }
            public string client_secret { get; set; }
        }

        private GfyCatOauthResponse Authentication { get; set; } = null;

        public async Task<string> GetAuthToken()
        {
            //Tru to initialize the authentication if it isn't set.
            if (Authentication == null)
            {
                Authentication = await GetOauthResponse();
                if (Authentication == null)
                {
                    return null;
                }
            }

            //try to re-generate the oauth info if expired
            if (Authentication.IsExpired())
            {
                Authentication = await GetOauthResponse();
                if (Authentication == null)
                {
                    return null;
                }
            }

            return Authentication.access_token;
        }

        /// <summary>
        /// Attempts to find client info in the database and authenticate with gfycat using it
        /// </summary>
        /// <returns></returns>
        public async Task<GfyCatOauthResponse> GetOauthResponse()
        {
            //TODO: reduce requests to database by storing a local copy
            //this should only request hourly but if there is no config in the database then it will constantly request.
            var config = Database.Load<GfycatClientInfo>("GfycatClientInfo");
            if (config == null)
            {
                return null;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.gfycat.com/v1/oauth/token");
            request.Content = new StringContent($"{{\"grant_type\":\"client_credentials\",\"client_id\":\"{config.client_id}\",\"client_secret\":\"{config.client_secret}\"}}",
                Encoding.UTF8,
                "application/json");

            var response = await Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var oauthObject = JsonConvert.DeserializeObject<GfyCatOauthResponse>(responseContent);
            return oauthObject;
        }

        /// <summary>
        /// Uses the gfycat api to find the correct url of a specific image.
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public async Task<string> GetGfyCatUrl(string original)
        {
            // Ensure that the provided content is a gfycat url,
            if (!original.Contains("gfycat.com/", StringComparison.InvariantCultureIgnoreCase))
            {
                return original;
            }

            // try to get the authenticated info.
            var authorization = await GetAuthToken();
            if (authorization == null)
            {
                //Default fallback to old fix method if the database there is no auth info setup.
                return FixGfycatUrl(original);
            }

            //make a copy of the original string so the content can be returned if there is a failed status code.
            var url = new string(original);

            //strip away the file extension and gfycat prefix from the url to get the direct content id
            var startIndex = url.IndexOf("gfycat.com/", StringComparison.InvariantCultureIgnoreCase) + "gfycat.com/".Length;
            url = url.Substring(startIndex, url.Length - startIndex);
            var extensionIndex = url.LastIndexOf(".");
            if (extensionIndex != -1)
            {
                url = url.Substring(0, extensionIndex);
            }

            //Make an authenticated get request to the api to get a json response for the gfycat content
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.gfycat.com/v1/gfycats/{url}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authorization);
            var response = await Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return original;
            }

            // Parse the responded content to get the gif url.
            var content = await response.Content.ReadAsStringAsync();
            var token = JToken.Parse(content);
            var responseUrl = token.Value<JToken>("gfyItem").Value<JToken>("gifUrl").ToString();
            return responseUrl;
        }

        /// <summary>
        /// Attempts to get the direct cdn url for the given gfycat url
        /// </summary>
        /// <param name="original">the original gfycat url</param>
        /// <returns></returns>
        public string FixGfycatUrl(string original)
        {
            if (original.Contains("gfycat", StringComparison.InvariantCultureIgnoreCase))
            {
                if (original.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase))
                {
                    original = original.Replace(".mp4", ".gif", StringComparison.InvariantCultureIgnoreCase);
                }
                else if (original.EndsWith(".webm", StringComparison.InvariantCultureIgnoreCase))
                {
                    original = original.Replace(".webm", ".gif", StringComparison.InvariantCultureIgnoreCase);
                }

                if (original.Contains("giant.", StringComparison.InvariantCultureIgnoreCase))
                {
                    return original;
                }
                else if (original.Contains("zippy.", StringComparison.InvariantCultureIgnoreCase))
                {
                    return original;
                }
                else if (original.Contains("thumbs.", StringComparison.InvariantCultureIgnoreCase))
                {
                    //Fixes cdn and replaces mobile or size restricted tags.
                    original = original.Replace("thumbs.", "giant.", StringComparison.InvariantCultureIgnoreCase);
                    if (original.Contains("-"))
                    {
                        int subStrIndex = original.IndexOf("-");
                        original = original.Substring(0, subStrIndex) + ".gif";
                    }
                    return original;
                }
                else
                {
                    var startIndex = original.IndexOf("gfycat", StringComparison.InvariantCultureIgnoreCase);
                    original = original.Substring(startIndex, original.Length - startIndex);
                    original = $"https://zippy.{original}.gif";
                    return original;
                }
            }

            return original;
        }
    }
}