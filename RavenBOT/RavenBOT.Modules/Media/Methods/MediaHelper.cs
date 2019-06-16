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

        public enum RequestHttpMethod
        {
            Get,

            Post
        }

        /// <summary>
        ///     Uses streamreader to get a string response from the requested url
        /// </summary>
        /// <param name="url">
        ///     The url.
        /// </param>
        /// <param name="headers">
        ///     The headers.
        /// </param>
        /// <param name="method">
        ///     Optional http request method, post/get
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        public async Task<string> GetResponseStringAsync(string url, IEnumerable<KeyValuePair<string, string>> headers = null, RequestHttpMethod method = RequestHttpMethod.Get)
        {
            using(var streamReader = new StreamReader(await GetResponseStreamAsync(url, headers, method).ConfigureAwait(false)))
            {
                return await streamReader.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Returns the response stream from the given url.
        /// </summary>
        /// <param name="url">
        ///     The url.
        /// </param>
        /// <param name="headers">
        ///     The headers.
        /// </param>
        /// <param name="method">
        ///     Optional http request method, post/get
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="Exception">
        ///     Throws if incorrect method input.
        /// </exception>
        private async Task<Stream> GetResponseStreamAsync(string url, IEnumerable<KeyValuePair<string, string>> headers = null, RequestHttpMethod method = RequestHttpMethod.Get)
        {
            Client.DefaultRequestHeaders.Clear();
            switch (method)
            {
                case RequestHttpMethod.Get:
                    if (headers == null)
                    {
                        return await Client.GetStreamAsync(url).ConfigureAwait(false);
                    }

                    foreach (var header in headers)
                    {
                        Client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }

                    return await Client.GetStreamAsync(url).ConfigureAwait(false);
                case RequestHttpMethod.Post:
                    FormUrlEncodedContent formContent = null;
                    if (headers != null)
                    {
                        formContent = new FormUrlEncodedContent(headers);
                    }

                    var message = await Client.PostAsync(url, formContent).ConfigureAwait(false);
                    return await message.Content.ReadAsStreamAsync().ConfigureAwait(false);
                default:
                    throw new Exception("That type of request is unsupported.");
            }
        }
    }
}