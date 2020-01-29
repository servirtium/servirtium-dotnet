using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        
        public static AspNetCoreServirtiumServer WithTransforms(IInteractionMonitor monitor, IInteractionTransforms interactionTransforms) =>
            new AspNetCoreServirtiumServer(Host.CreateDefaultBuilder(), monitor, interactionTransforms);
        public static AspNetCoreServirtiumServer Default(IInteractionMonitor monitor, Uri serviceHost) =>
            new AspNetCoreServirtiumServer(Host.CreateDefaultBuilder(), monitor, new SimpleInteractionTransforms(serviceHost, new string[0], new string[0]));
        public static AspNetCoreServirtiumServer WithCommandLineArgs(string[] args, IInteractionMonitor monitor, IInteractionTransforms interactionTransforms) =>
            new AspNetCoreServirtiumServer(Host.CreateDefaultBuilder(args), monitor, interactionTransforms);

        //private static readonly 
        public AspNetCoreServirtiumServer(IHostBuilder hostBuilder, IInteractionMonitor monitor, IInteractionTransforms interactionTransforms)
        {
            HashSet<string> headersNotToTransfer = new HashSet<string> {};
            _host = hostBuilder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.Configure(app =>
                {
                    app.Run(async ctx =>
                    {
                        var targetHost = new Uri($"{ctx.Request.Scheme}{Uri.SchemeDelimiter}{ctx.Request.Host}");
                        var requestInteraction = new MarkdownInteraction.Builder()
                            .Number(_interactionCounter.Bump())
                            .Method(new System.Net.Http.HttpMethod(ctx.Request.Method))
                            .Path($"{ctx.Request.Path}{ctx.Request.QueryString}")
                            //Remap headers from a dictionary of string lists to a list of (string, string) tuples
                            .RequestHeaders(ctx.Request.Headers.SelectMany(
                                kvp =>
                                    kvp.Value.Select(val => (kvp.Key, val))))
                            .Build();
                        var serviceRequestInteraction = interactionTransforms.TransformClientRequestForRealService(requestInteraction);
                        var responseFromService = await monitor.GetServiceResponseForRequest(
                            targetHost,
                            serviceRequestInteraction,
                            false);
                        /* Transferring headers breaks response for now, can fix later as this is just proving the pipeline.
                         * ctx.Response.OnStarting(() => {7
                            foreach (var headerNameAndValue in responseFromService.Headers.Where(h=>!headersNotToTransfer.Contains(h.Item1.ToLower())))
                            {
                                (string name, string value) = headerNameAndValue;
                                if (ctx.Response.Headers.TryGetValue(name, out var existing))
                                {
                                    ctx.Response.Headers[name] = new StringValues(existing.Append(value).ToArray());
                                }
                                else
                                {
                                    ctx.Response.Headers.Add(name, value);
                                }
                            }
                            return Task.CompletedTask;
                        });*/

                        ctx.Response.OnCompleted(() =>
                        {
                            Console.WriteLine($"{requestInteraction.Method} Request to {targetHost}{requestInteraction.Path} returned to client with code {ctx.Response.StatusCode}");
                            return Task.CompletedTask;
                        });

                        if (responseFromService.Body != null)
                        {
                            await ctx.Response.WriteAsync(responseFromService.Body.ToString());
                        }
                        else
                        {
                            await ctx.Response.CompleteAsync();
                        }
                    });

                });
            }).Build();

        }

        public void FinishedScript()
        {
            throw new NotImplementedException();
        }

        public async Task<IServirtiumServer> Start()
        {
            await _host.StartAsync();
            return this;
        }

        public async Task Stop()
        {
            await _host.StopAsync();
        }
    }
}
