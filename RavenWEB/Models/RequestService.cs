using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RavenWEB.Models;

namespace RavenWEB.Services
{
    public class RequestService
    {
        public static T GetRequest<T>(string uri, string accessToken)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "Get";
            request.ContentLength = 0;
            request.Headers.Add("Authorization", "Bearer " + accessToken);
            request.ContentType = "application/x-www-form-urlencoded";
 
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                StreamReader streamReader = new StreamReader(response.GetResponseStream());
                try
                {
                    var model = JsonConvert.DeserializeObject<T>(streamReader.ReadToEnd());
                    return model;
                }
                catch (Exception e)
                {
                    return default(T);
                }
            }
        }

        public static UserModel GetUser(string accessToken)
        {
            return GetRequest<UserModel>("https://discordapp.com/api/users/@me", accessToken);
        }


        public static GuildModel[] GetGuilds(string accessToken)
        {
            return GetRequest<GuildModel[]>("https://discordapp.com/api/users/@me/guilds", accessToken);
        }
    }
}
