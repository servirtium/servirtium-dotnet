using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Servirtium.Core;

namespace Servirtium.AspNetCore
{
    public class AspNetCoreServirtiumServer : IServirtiumServer
    {
        private readonly IHost _host;

        private readonly InteractionCounter _interactionCounter = new InteractionCounter();

        private readonly IInteractionMonitor _interactionMonitor;
      
        public static AspNetCoreServirtiumServer WithTransforms(int port, IInteractionMonitor monitor, IInteractionTransforms interactionTransforms) =>
            new AspNetCoreServirtiumServer(Host.CreateDefaultBuilder(), monitor, interactionTransforms, port);
        public static AspNetCoreServirtiumServer Default(int port, IInteractionMonitor monitor, Uri serviceHost) =>
            new AspNetCoreServirtiumServer(Host.CreateDefaultBuilder(), monitor, new SimpleInteractionTransforms(serviceHost), port);
        public static AspNetCoreServirtiumServer WithCommandLineArgs(string[] args, IInteractionMonitor monitor, IInteractionTransforms interactionTransforms) =>
            new AspNetCoreServirtiumServer(Host.CreateDefaultBuilder(args), monitor, interactionTransforms, null);

        //private static readonly 
        private AspNetCoreServirtiumServer(IHostBuilder hostBuilder, IInteractionMonitor monitor, IInteractionTransforms interactionTransforms, int? port)
        {
            _interactionMonitor = monitor;
            _host = hostBuilder.ConfigureWebHostDefaults(webBuilder =>
            {                    
                if (port != null)
                {
                    //If a port is specified, override urls with specified port, listening on all available hosts, for HTTP.
                    webBuilder.UseUrls($"http://*:{port}");
                }
                webBuilder.Configure(app =>
                {
                    app.Run(async ctx =>
                    {
                        var targetHost = new Uri($"{ctx.Request.Scheme}{Uri.SchemeDelimiter}{ctx.Request.Host}");
                        var requestBuilder = new ImmutableInteraction.Builder()
                            .Number(_interactionCounter.Bump())
                            .Method(new System.Net.Http.HttpMethod(ctx.Request.Method))
                            .Path($"{ctx.Request.Path}{ctx.Request.QueryString}")
                            //Remap headers from a dictionary of string lists to a list of (string, string) tuples
                            .RequestHeaders
                            (
                                ctx.Request.Headers
                                    .SelectMany(kvp => kvp.Value.Select(val => (kvp.Key, val)))
                                    .ToArray()
                            );
                        if (!String.IsNullOrWhiteSpace(ctx.Request.ContentType))
                        {
                            var bodyString = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
                            requestBuilder.RequestBody(bodyString, MediaTypeHeaderValue.Parse(ctx.Request.ContentType));
                        }
                        var requestInteraction = requestBuilder.Build();
                        var serviceRequestInteraction = interactionTransforms.TransformClientRequestForRealService(requestInteraction);
                        var responseFromService = await monitor.GetServiceResponseForRequest(
                            targetHost,
                            serviceRequestInteraction,
                            false);
                        var clientResponse = interactionTransforms.TransformRealServiceResponseForClient(responseFromService);
                        monitor.NoteCompletedInteraction(serviceRequestInteraction, clientResponse);
                        //Always remove the 'Transfer-Encoding: chunked' header if present.
                        //If it's present in the response.Headers collection ast this point, Kestrel expects you to add chunk notation to the body yourself
                        //However if you just send it with no content-length, Kestrel will add the chunked header and chunk the body for you.
                        clientResponse = clientResponse
                            .WithRevisedHeaders(
                                clientResponse.Headers
                                    .Where((h)=>!(h.Name.ToLower()=="transfer-encoding" && h.Value.ToLower()=="chunked")))
                            .WithReadjustedContentLength();
                        ctx.Response.OnCompleted(() =>
                        {
                            Console.WriteLine($"{requestInteraction.Method} Request to {targetHost}{requestInteraction.Path} returned to client with code {ctx.Response.StatusCode}");
                            return Task.CompletedTask;
                        });

                        ctx.Response.StatusCode = (int)clientResponse.StatusCode;

                        //Transfer adjusted headers to the response going out to the client
                        foreach ((string headerName, string headerValue) in clientResponse.Headers)
                        { 
                            if (ctx.Response.Headers.TryGetValue(headerName, out var headerInResponse))
                            {
                                ctx.Response.Headers[headerName] = new StringValues(headerInResponse.Append(headerValue).ToArray());
                            }
                            else 
                            {
                                ctx.Response.Headers[headerName] = new StringValues(headerValue);
                            }
                        }
                        if (clientResponse.Body != null)
                        {
                            await ctx.Response.WriteAsync(clientResponse.Body.ToString());
                        }
                        await ctx.Response.CompleteAsync();
                    });

                });
            }).Build();

        }

        public void FinishedScript()
        {
            _interactionMonitor.FinishedScript(_interactionCounter.Get(), false);
        }

        public async Task<IServirtiumServer> Start()
        {
            await _host.StartAsync();
            return this;
        }

        public async Task Stop()
        {
            FinishedScript();
            await _host.StopAsync();
        }
    }
}
