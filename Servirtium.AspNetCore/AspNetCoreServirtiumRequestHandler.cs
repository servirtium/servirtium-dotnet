using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Servirtium.Core;


[assembly: InternalsVisibleTo("Servirtium.AspNetCore.Tests")]
namespace Servirtium.AspNetCore
{
    public class AspNetCoreServirtiumRequestHandler
    {
        private readonly IInteractionMonitor _monitor;
        private readonly IInteractionTransforms _interactionTransforms;
        private readonly InteractionCounter _interactionCounter;

        internal AspNetCoreServirtiumRequestHandler(
            IInteractionMonitor monitor,
            IInteractionTransforms interactionTransforms,
            InteractionCounter interactionCounter,
            ICollection<IInteraction.Note> notes
            )
        {
            _monitor = monitor;
            _interactionTransforms = interactionTransforms;
            _interactionCounter = interactionCounter;

            HandleRequestDelegate = async ctx =>
            {
                var targetHost = new Uri($"{ctx.Request.Scheme}{Uri.SchemeDelimiter}{ctx.Request.Host}");
                var pathAndQuery = $"{ctx.Request.Path}{ctx.Request.QueryString}";                
                
                ctx.Response.OnCompleted(() =>
                {
                    Console.WriteLine($"{ctx.Request.Method} Request to {targetHost}{pathAndQuery} returned to client with code {ctx.Response.StatusCode}");
                    return Task.CompletedTask;
                });

                await HandleRequest(targetHost, pathAndQuery, ctx.Request.Method, ctx.Request.Headers, ctx.Request.ContentType, ctx.Request.Body, (code) => ctx.Response.StatusCode = (int)code, ctx.Response.Headers, ctx.Response.Body, new List<IInteraction.Note>(notes));
                
                await ctx.Response.CompleteAsync();
            };
        }

        internal async Task HandleRequest(
            Uri targetHost, string pathAndQuery, string method, IHeaderDictionary requestHeaders, string? requestContentType, Stream? requestBodyStream, Action<HttpStatusCode> statusCodeSetter, IHeaderDictionary responseHeaders, Stream responseBodyStream, ICollection<IInteraction.Note> notes)
        {
            var requestBuilder = new ImmutableInteraction.Builder()
                .Number(_interactionCounter.Bump())
                .Method(new HttpMethod(method))
                .Path(pathAndQuery)
                //Remap headers from a dictionary of string lists to a list of (string, string) tuples
                .RequestHeaders
                (
                    requestHeaders
                        .SelectMany(kvp => kvp.Value.Select(val => (kvp.Key, val)))
                        .ToArray()
                )
                .Notes(notes);


            if (!String.IsNullOrWhiteSpace(requestContentType)&& requestBodyStream!=null)
            {
                var bodyString = await new StreamReader(requestBodyStream).ReadToEndAsync();
                requestBuilder.RequestBody(bodyString, MediaTypeHeaderValue.Parse(requestContentType));
            }
            var requestInteraction = requestBuilder.Build();
            var serviceRequestInteraction = _interactionTransforms.TransformClientRequestForRealService(requestInteraction);
            var responseFromService = await _monitor.GetServiceResponseForRequest(
                targetHost,
                serviceRequestInteraction,
                false);
            var clientResponse = _interactionTransforms.TransformRealServiceResponseForClient(responseFromService);
            _monitor.NoteCompletedInteraction(serviceRequestInteraction, clientResponse);
            //Always remove the 'Transfer-Encoding: chunked' header if present.
            //If it's present in the response.Headers collection ast this point, Kestrel expects you to add chunk notation to the body yourself
            //However if you just send it with no content-length, Kestrel will add the chunked header and chunk the body for you.
            clientResponse = clientResponse
                 .WithRevisedHeaders(
                     clientResponse.Headers
                         .Where((h) => !(h.Name.ToLower() == "transfer-encoding" && h.Value.ToLower() == "chunked")))
                 .WithReadjustedContentLength();


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
            if (clientResponse.Body != null)
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(clientResponse.Body?.ToString() ?? "");
                await responseBodyStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
            }
        }

        internal RequestDelegate HandleRequestDelegate { get;  }
    }
}
