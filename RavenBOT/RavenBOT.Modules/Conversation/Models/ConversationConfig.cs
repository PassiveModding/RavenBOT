using Newtonsoft.Json;

namespace RavenBOT.Modules.Conversation.Models
{
    public class ConversationConfig
    {
        public static string DocumentName()
        {
            return $"ConversationConfig";
        }

        [JsonIgnore]
        public string ApiJson => JsonConvert.SerializeObject(Certificate);

        public GoogleCert Certificate { get; set; } = new GoogleCert();
        public class GoogleCert
        {
            public string type { get; set; }
            public string project_id { get; set; }
            public string private_key_id { get; set; }
            public string private_key { get; set; }
            public string client_email { get; set; }
            public string client_id { get; set; }
            public string auth_uri { get; set; }
            public string token_uri { get; set; }
            public string auth_provider_x509_cert_url { get; set; }
            public string client_x509_cert_url { get; set; }
        }
    }
}