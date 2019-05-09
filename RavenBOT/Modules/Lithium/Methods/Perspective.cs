using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace RavenBOT.Modules.Lithium.Methods
{
    public class Perspective
    {
        public class AnalyzeCommentRequest
        {
            public string clientToken;

            public Comment comment;

            public bool doNotStore;

            public string[] languages = { "en" };

            public Dictionary<string, RequestedAttributes> requestedAttributes;

            public AnalyzeCommentRequest(string comment, Dictionary<string, RequestedAttributes> requestedAttributeses = null, bool doNotStore = true, string clienttoken = null)
            {
                this.comment = new Comment(comment);
                this.doNotStore = doNotStore;
                if (requestedAttributeses == null)
                    requestedAttributes.Add("TOXICITY", new RequestedAttributes());
                else
                    requestedAttributes = requestedAttributeses;
                clientToken = clienttoken;
            }
        }

        public class AnalyzeCommentResponse
        {
            public AttributeScores attributeScores { get; set; }

            public List<string> languages { get; set; }

            public class AttributeScores
            {
                public _TOXICITY TOXICITY { get; set; }

                public class _TOXICITY
                {
                    public List<SpanScore> spanScores { get; set; }

                    public SummaryScore summaryScore { get; set; }

                    public class SpanScore
                    {
                        public int begin { get; set; }

                        public int end { get; set; }

                        public Score score { get; set; }

                        public class Score
                        {
                            public string type { get; set; }

                            public double value { get; set; }
                        }
                    }

                    public class SummaryScore
                    {
                        public string type { get; set; }

                        public double value { get; set; }
                    }
                }
            }
        }

        public class Api
        {
            public Api(string apiKey)
            {
                BaseAddress = new Uri($"https://commentanalyzer.googleapis.com/v1alpha1/comments:analyze?key={apiKey}");
                Client = new HttpClient
                {
                    BaseAddress = BaseAddress
                };
            }

            private Uri BaseAddress { get; }

            public HttpClient Client { get; set; }

            public string GetResponseString(AnalyzeCommentRequest request)
            {
                    var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                    var response = Client.PostAsync(BaseAddress, content).Result;
                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStringAsync().Result;
            }

            /// <summary>
            /// Gets the message's toxicity score from 0 to 100
            /// </summary>
            /// <param name="input">the input message</param>
            /// <returns>a toxicity score from 0 to 100</returns>
            public double GetToxicityScore(string input)
            {
                var res = QueryToxicity(input);
                return res.attributeScores.TOXICITY.summaryScore.value * 100;
            }

            public AnalyzeCommentResponse QueryToxicity(string input)
            {
                var requestedAttributeses = new Dictionary<string, RequestedAttributes> { { "TOXICITY", new RequestedAttributes() } };
                var req = new AnalyzeCommentRequest(input, requestedAttributeses);
                var res = SendRequest(req);
                return res;
            }

            public AnalyzeCommentResponse SendRequest(AnalyzeCommentRequest request)
            {
                    var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                    var response = Client.PostAsync(BaseAddress, content).Result;
                    response.EnsureSuccessStatusCode();
                    var data = response.Content.ReadAsStringAsync().Result;
                    var result = JsonConvert.DeserializeObject<AnalyzeCommentResponse>(data);
                    return result;
            }
        }

        public class Comment
        {
            public Comment(string text, string type = "PLAIN_TEXT")
            {
                this.text = text;
                this.type = type;
            }

            public string text { get; set; }

            public string type { get; set; }

            public static implicit operator string(Comment v)
            {
                throw new NotImplementedException();
            }
        }

        public class RequestedAttributes
        {
            public float scoreThreshold;

            public string scoreType;

            public RequestedAttributes(string scoretype = "PROBABILITY", float scorethreshold = 0)
            {
                scoreType = scoretype;
                scoreThreshold = scorethreshold;
            }
        }
    }
}