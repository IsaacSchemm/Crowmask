﻿using Crowmask.Formats.ContentNegotiation;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Crowmask
{
    /// <summary>
    /// Provides methods that make it easier to return ActivityStreams,
    /// Markdown, or HTML responses from Azure Functions endpoints.
    /// </summary>
    public static class HttpRequestDataExtensions
    {
        /// <summary>
        /// Writes the given string to the HTTP response, with a Content-Type
        /// header derived from the given CrowmaskFormat.
        /// </summary>
        /// <param name="req">The HTTP request</param>
        /// <param name="format">The format to use (Markdown, HTML, ActivityStreams, RSS, or Atom)</param>
        /// <param name="content">The string content (after any serialization)</param>
        /// <returns></returns>
        public static async Task<HttpResponseData> WriteCrowmaskResponseAsync(
            this HttpRequestData req,
            CrowmaskFormat format,
            string content)
        {
            var resp = req.CreateResponse(HttpStatusCode.OK);
            resp.Headers.Add("Content-Type", $"{format.MediaType}; charset=utf-8");
            await resp.WriteStringAsync(content, Encoding.UTF8);
            return resp;
        }

        /// <summary>
        /// Determine which formats should be used for the given request, in order of preference.
        /// </summary>
        /// <param name="req">The HTTP request</param>
        /// <returns>Supported formats (Markdown, HTML, ActivityStreams, RSS, or Atom), in order of preference based on the Accept header</returns>
        public static IEnumerable<CrowmaskFormat> GetAcceptableCrowmaskFormats(this HttpRequestData req)
        {
            var headers = req.Headers.GetValues("Accept")
                .Select(str => MediaTypeHeaderValue.Parse(str))
                .OrderByDescending(h => h, MediaTypeHeaderValueComparer.QualityComparer);
            foreach (var accept in headers)
                foreach (var format in CrowmaskFormat.All)
                    foreach (var type in format.MediaTypes)
                        if (type.IsSubsetOf(accept))
                            yield return format;
        }
    }
}
