using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RavenWEB.Extensions
{
    public static class RequestExtensions
    {
        public static string GetCurrentUri(this HttpRequest request)
        {
            var path = $"{request.Scheme}://{request.Host.ToUriComponent()}{request.PathBase.ToUriComponent()}{request.Path.ToUriComponent()}{request.QueryString.ToUriComponent()}";
            return path;
        }

        public static string GetBaseUri(this HttpRequest request)
        {
            var path = $"{request.Scheme}://{request.Host.ToUriComponent()}{request.PathBase.ToUriComponent()}";
            return path;
        }
    }
}
