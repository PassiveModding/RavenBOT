using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Cloud.Dialogflow.V2;
using Newtonsoft.Json;

namespace RavenBOT.Modules.Conversation.Methods
{
    public class ConversationFunctions
    {
        public bool TryInvoke(string methodName, ref ConversationResponse response, string payload = null)
        {
            response = null;
            MethodInfo mi = this.GetType().GetMethod(methodName);
            if (mi != null)
            {
                var res = mi.Invoke(this, new object[]{ payload });
                if (res is ConversationResponse convResponse)
                {
                    response = convResponse;
                    return true;
                }
            }

            return false;
        }

        public class ConversationResponse
        {
            public ConversationResponse(string response)
            {
                Value = response;
            }
            public string Value {get;set;}
        }

        public ConversationResponse GetWeather(string payload)
        {
            if (payload == null)
            {
                return null;
            }

            var responseObject = JsonConvert.DeserializeObject<WeatherPayload>(payload);

            return new ConversationResponse(responseObject.Location);
        }

        public class WeatherPayload
        {
            public string Location {get;set;}
        }

        public List<string> GetFunctions()
        {
            return this.GetType().GetMethods().Where(x => x.ReturnType == typeof(ConversationResponse)).Select(x => x.Name).ToList();
        }
    }
}