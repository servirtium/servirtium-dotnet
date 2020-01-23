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
        public static AspNetCoreServirtiumServer Default(IInteractionMonitor monitor) =>
            new AspNetCoreServirtiumServer(Host.CreateDefaultBuilder(), monitor);
        public static AspNetCoreServirtiumServer WithCommandLineArgs(string[] args, IInteractionMonitor monitor) =>
            new AspNetCoreServirtiumServer(Host.CreateDefaultBuilder(args), monitor);

        //private static readonly 
        public AspNetCoreServirtiumServer(IHostBuilder hostBuilder, IInteractionMonitor monitor)
        {
            HashSet<string> headersNotToTransfer = new HashSet<string> {};
            _host = hostBuilder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.Configure(app => {
                        app.Run(async ctx =>
                        {
                            var targetUri = new Uri($"{ctx.Request.Scheme}{Uri.SchemeDelimiter}{ctx.Request.Host}{ctx.Request.Path}{ctx.Request.QueryString}");
                            var responseFromService = await monitor.GetServiceResponseForRequest(
                                ctx.Request.Method,
                                targetUri,
                                Interaction.Noop,
                                false);
                            /* Transferring headers breaks response for now, can fix later as this is just proving the pipeline.
                             * ctx.Response.OnStarting(() => {
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

                            ctx.Response.OnCompleted(() => {
                                Console.WriteLine($"Request to {targetUri} returned to client with code {ctx.Response.StatusCode}");
                                return Task.CompletedTask;
                            });
                            try
                            {

                                await ctx.Response.WriteAsync(responseFromService.Body.ToString() ?? "");
                            }
                            catch (Exception ex)
                            {
                                throw;
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
