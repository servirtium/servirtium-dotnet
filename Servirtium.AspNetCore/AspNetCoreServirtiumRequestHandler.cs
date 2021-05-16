using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Servirtium.Core;
using Servirtium.Core.Http;
using Servirtium.Core.Interactions;

[assembly: InternalsVisibleTo("Servirtium.AspNetCore.Tests")]
namespace Servirtium.AspNetCore
{
    internal class AspNetCoreServirtiumRequestHandler
    {
        private readonly IServirtiumRequestHandler _internalHandler;

        private readonly ILogger<AspNetCoreServirtiumRequestHandler> _logger;

        internal AspNetCoreServirtiumRequestHandler(
            IServirtiumRequestHandler internalHandler,
            ILoggerFactory loggerFactory
            )
        {
            _internalHandler = internalHandler;
            _logger = loggerFactory.CreateLogger<AspNetCoreServirtiumRequestHandler>();
        }

        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string? ToString()
        {
            return base.ToString();
        }

        internal async Task HandleRequest(
            Uri targetHost, 
            string pathAndQuery, 
            string method, 
            IHeaderDictionary requestHeaders, 
            string? requestContentType, 
            Stream? requestBodyStream, 
            Action<HttpStatusCode> statusCodeSetter, 
            IHeaderDictionary responseHeaders, 
            Stream responseBodyStream,
            Action<string> responseContentTypeSetter,
            IEnumerable<IInteraction.Note> notes)
        {
            var headers = requestHeaders
                        .SelectMany(kvp => kvp.Value.Select(val => (kvp.Key, val)))
                        .ToArray();

            var requestBuilder = new ServiceRequest.Builder()
                .Method(new HttpMethod(method))
                .Url(new Uri(targetHost, pathAndQuery))
                //Remap headers from a dictionary of string lists to a list of (string, string) tuples
                .Headers(headers);


            if (!String.IsNullOrWhiteSpace(requestContentType) && requestBodyStream!=null)
            {
                using (var ms = new MemoryStream())
                {
                    await requestBodyStream.CopyToAsync(ms);
                    requestBuilder.Body(ms.ToArray(), MediaTypeHeaderValue.Parse(requestContentType));
                }
            }
            var request = requestBuilder.Build();
            _logger.LogDebug($"Processing {request.Method} Request to {request.Url}.");
            var clientResponse = await _internalHandler.ProcessRequest(request, notes);
            _logger.LogDebug($"Processed {request.Method} request to {request.Url}, result: {clientResponse.StatusCode}.");

            //Always remove the 'Transfer-Encoding: chunked' header if present.
            //If it's present in the response.Headers collection ast this point, Kestrel expects you to add chunk notation to the body yourself
            //However if you just send it with no content-length, Kestrel will add the chunked header and chunk the body for you.
            clientResponse = new ServiceResponse.Builder()
                .From(clientResponse)
                .Headers(
                     clientResponse.Headers
                         .Where((h) => !(h.Name.ToLower() == "transfer-encoding" && h.Value.ToLower() == "chunked")))
                .FixContentLength()
                .Build();
            statusCodeSetter(clientResponse.StatusCode);

            //Transfer adjusted headers to the response going out to the client
            foreach ((string headerName, string headerValue) in clientResponse.Headers)
            {
                if (responseHeaders.TryGetValue(headerName, out var headerInResponse))
                {
                    responseHeaders[headerName] = new StringValues(headerInResponse.Append(headerValue).ToArray());
                }
                else
                {
                    responseHeaders[headerName] = new StringValues(headerValue);
                }
            }
            if (clientResponse.Body.HasValue)
            {
                responseContentTypeSetter(clientResponse.Body.Value.Type.MediaType);
                await responseBodyStream.WriteAsync(clientResponse.Body.Value.Content, 0, clientResponse.Body.Value.Content.Length);
            }
        }

        public void StartScript() {
            _internalHandler.StartScript();
        }
    }
}
