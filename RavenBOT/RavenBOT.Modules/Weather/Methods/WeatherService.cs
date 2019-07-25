using System.IO;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RavenBOT.Common;
using RavenBOT.Modules.Weather.Models;

namespace RavenBOT.Modules.Weather.Methods
{
    public class WeatherService : IServiceable
    {
        public WeatherService(HttpClient client, IDatabase db, LogHandler logger)
        {
            Client = client;
            var config = db.Load<DarkSkyConfig>(DarkSkyConfig.DocumentName());
            if (config != null)
            {
                ApiKey = config.ApiKey;
            }
            else
            {
                logger.Log("Weather api key not set, Get a darksky api key here: https://darksky.net/dev and set it using the Weather SetApiKey command.", Discord.LogSeverity.Warning);
            }
            Database = db;
        }

        public HttpClient Client { get; }
        public IDatabase Database { get; }
        public string ApiKey { get; set; }

        public async Task<JArray> GeocodeAsync(string request)
        {
            var url = $"http://nominatim.openstreetmap.org/search/{request}?format=json";
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, Uri.EscapeUriString(url));
            httpRequest.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
            httpRequest.Headers.Add("Accept", "application/json");
            var response = await Client.SendAsync(httpRequest);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JArray.Parse(content);
        }

        public async Task<(string, DarkSkyResponse)> GetWeatherAsync(JToken GeocodeResponse)
        {

            var baseUrl = $"https://api.darksky.net/forecast/{ApiKey}/";

            var name = GeocodeResponse["display_name"];
            
            if (ApiKey == null)
            {
                return (name.ToString(), null);
            }

            var lat = GeocodeResponse["lat"];
            var lon = GeocodeResponse["lon"];
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, Uri.EscapeUriString($"{baseUrl}{lat},{lon}"));
            httpRequest.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
            httpRequest.Headers.Add("Accept", "application/json");
            var darkSkyResponse = await Client.SendAsync(httpRequest);
            if (!darkSkyResponse.IsSuccessStatusCode)
            {
                return (name.ToString(), null);
            }
            var response = await darkSkyResponse.Content.ReadAsStringAsync();
            var dsr = DarkSkyResponse.FromJson(response);
            return (name.ToString(), dsr);
        }

        public double FtoC(double f)
        {
            return Math.Round((f-32)*(5.0/9), 1);
        }

        public DateTimeOffset UnixTime(long time, TimeSpan offset)
        {
            return DateTimeOffset.FromUnixTimeSeconds(time).ToOffset(offset);
        }
    }
}