using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Servirtium.Core;
using Servirtium.Core.Http;
using Servirtium.Core.Interactions;

[assembly: InternalsVisibleTo("Servirtium.AspNetCore.Tests")]
namespace Servirtium.AspNetCore
{
    public class AspNetCoreServirtiumServer : IServirtiumServer
    {
        private readonly IHost _host;

        private readonly ILogger<AspNetCoreServirtiumServer> _logger;
        
        private readonly IServirtiumRequestHandler _servirtiumRequestHandler;

        IServirtiumRequestHandler IServirtiumServer.InternalRequestHandler => _servirtiumRequestHandler;

        private readonly ICollection<IInteraction.Note> _notesForNextInteraction = new LinkedList<IInteraction.Note>();
      
        public static AspNetCoreServirtiumServer WithTransforms(int port, IInteractionMonitor monitor, IHttpMessageTransforms interactionTransforms, ILoggerFactory? loggerFactory = null) =>
            new AspNetCoreServirtiumServer(Host.CreateDefaultBuilder(), new InteractionRecordingServirtiumRequestHandler(interactionTransforms, monitor), port, loggerFactory);
        public static AspNetCoreServirtiumServer Default(int port, IInteractionMonitor monitor, Uri serviceHost, ILoggerFactory? loggerFactory = null) =>
            WithTransforms(port, monitor, new SimpleHttpMessageTransforms(serviceHost), loggerFactory);
        public static AspNetCoreServirtiumServer WithCommandLineArgs(string[] args, IInteractionMonitor monitor, IHttpMessageTransforms interactionTransforms, ILoggerFactory? loggerFactory = null) =>
            new AspNetCoreServirtiumServer(Host.CreateDefaultBuilder(args), new InteractionRecordingServirtiumRequestHandler(interactionTransforms, monitor), null, loggerFactory);

        //private static readonly 
        internal AspNetCoreServirtiumServer(IHostBuilder hostBuilder, IServirtiumRequestHandler servirtiumHandler, int? port, ILoggerFactory? loggerFactoryParameter = null)
        {
            var loggerFactory = loggerFactoryParameter ?? NullLoggerFactory.Instance;
            _logger = loggerFactory.CreateLogger<AspNetCoreServirtiumServer>();
            _logger.LogInformation("Starting AspNetCoreServirtiumServer");
            var envs = Environment.GetEnvironmentVariables();
            _logger.LogDebug($"Environment variables:");
            foreach(var key in envs.Keys)
            {
                if (key!=null)
                {
                    _logger.LogDebug($"{key} = {envs[key]}");
                }
            }
            _servirtiumRequestHandler = servirtiumHandler;

            _host = hostBuilder.ConfigureWebHostDefaults(webBuilder =>
            {
                if (port != null)
                {
                    //If a port is specified, override urls with specified port, listening on all available hosts, for HTTP.
                    webBuilder.UseUrls($"http://*:{port}");
                }
                webBuilder.Configure(app =>
                {
                    var handler = new AspNetCoreServirtiumRequestHandler(servirtiumHandler, loggerFactory);
                    app.Run(async ctx =>
                    {
                        if (HttpMethods.IsOptions(ctx.Request.Method))
                        {
                            //Kill CORS in the face, bypass any Servirtium logic.
                            ctx.Response.Headers.Append("Access-Control-Allow-Origin", new StringValues("*"));
                            ctx.Response.Headers.Append("Access-Control-Allow-Methods", new StringValues("*"));
                            ctx.Response.Headers.Append("Access-Control-Allow-Headers", new StringValues("*"));
                            ctx.Response.Headers.Append("Access-Control-Allow-Credentials", new StringValues(new string[] { "true", ctx.Request.Host.Value }));
                            ctx.Response.Headers.Append("Access-Control-Max-Age", new StringValues("864000"));

                        }
                        else
                        {
                            var targetHost = new Uri($"{ctx.Request.Scheme}{Uri.SchemeDelimiter}{ctx.Request.Host}");
                            var pathAndQuery = $"{ctx.Request.Path}{ctx.Request.QueryString}";

                            ctx.Response.OnCompleted(() =>
                            {
                                _logger.LogInformation($"{ctx.Request.Method} request to {targetHost}{pathAndQuery} returned to client with code {ctx.Response.StatusCode}");
                                return Task.CompletedTask;
                            });
                            List<IInteraction.Note> notes;
                            lock (_notesForNextInteraction)
                            {
                                notes = new List<IInteraction.Note>(_notesForNextInteraction);
                                _notesForNextInteraction.Clear();
                            }
                            await handler.HandleRequest(targetHost, pathAndQuery, ctx.Request.Method, ctx.Request.Headers, ctx.Request.ContentType, ctx.Request.Body, (code) => ctx.Response.StatusCode = (int)code, ctx.Response.Headers, ctx.Response.Body, (ct)=> ctx.Response.ContentType=ct, notes);

                        }
                        await ctx.Response.CompleteAsync();
                    });
                });

            }).Build();

        }

        public async Task<IServirtiumServer> Start()
        {
            _logger.LogDebug($"Host starting.");
            await _host.StartAsync();
            _logger.LogInformation($"Host successfully started.");
            return this;
        }

        public async Task Stop()
        {
            ((IServirtiumServer)this).FinishedScript();
            await _host.StopAsync();
            _host.Dispose();
            _logger.LogInformation($"Host successfully stopped.");
            
        }

        public void MakeNote(string title, string note)
        {
            _logger.LogDebug($"Making text note, title: '{title}'.");
            _notesForNextInteraction.Add(new IInteraction.Note(IInteraction.Note.NoteType.Text, title, note));
        }

        public void MakeCodeNote(string title, string code)
        {
            _logger.LogDebug($"Making code note, title: '{title}'.");
            _notesForNextInteraction.Add(new IInteraction.Note(IInteraction.Note.NoteType.Code, title, code));
        }
    }
}
