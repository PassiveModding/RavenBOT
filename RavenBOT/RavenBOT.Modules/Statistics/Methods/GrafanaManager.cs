using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RavenBOT.Modules.Statistics.Models;
using RavenBOT.Common;
using RavenBOT.Common.Interfaces;

namespace RavenBOT.Modules.Statistics.Methods
{
    public class GrafanaManager : IServiceable
    {
        public IDatabase Database { get; }
        public HttpClient Client { get; }

        public GrafanaManager(IDatabase database)
        {
            Database = database;
            Client = new HttpClient();
        }

        public void SaveGrafanaConfig(GrafanaConfig config)
        {
            Database.Store(config, GrafanaConfig.DocumentName());
        }

        public GrafanaConfig GetGrafanaConfig()
        {
            var config = Database.Load<GrafanaConfig>(GrafanaConfig.DocumentName());
            if (config == null)
            {
                config = new GrafanaConfig();
                Database.Store(config, GrafanaConfig.DocumentName());
            }
            return config;
        }

        public HttpRequestMessage GetRequest(GrafanaConfig config, string url)
        {
            return new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                    RequestUri = new System.Uri(url),
                    Headers = { { HttpRequestHeader.Authorization.ToString(), $"Bearer {config.ApiKey}" },
                        { HttpRequestHeader.Accept.ToString(), "application/json" }
                        }
            };
        }

        public async Task<HttpContent> GetHttpContentAsync(HttpRequestMessage request)
        {
            var response = await Client.SendAsync(request);
            return response.Content;
        }
        public async Task<T> RequestAndDeserializeAsync<T>(HttpContent content)
        {
            var responseString = await content.ReadAsStringAsync();
            var responseClass = JsonConvert.DeserializeObject<T>(responseString);
            return responseClass;
        }
    }
}