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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Servirtium.Core;


[assembly: InternalsVisibleTo("Servirtium.AspNetCore.Tests")]
namespace Servirtium.AspNetCore
{
    public class AspNetCoreServirtiumServer : IServirtiumServer
    {
        private readonly IHost _host;

        private readonly InteractionCounter _interactionCounter = new InteractionCounter();

        private readonly IInteractionMonitor _interactionMonitor;

        private readonly ICollection<IInteraction.Note> _notesForNextInteraction = new LinkedList<IInteraction.Note>();
      
        public static AspNetCoreServirtiumServer WithTransforms(int port, IInteractionMonitor monitor, IInteractionTransforms interactionTransforms) =>
            new AspNetCoreServirtiumServer(Host.CreateDefaultBuilder(), monitor, interactionTransforms, port);
        public static AspNetCoreServirtiumServer Default(int port, IInteractionMonitor monitor, Uri serviceHost) =>
            new AspNetCoreServirtiumServer(Host.CreateDefaultBuilder(), monitor, new SimpleInteractionTransforms(serviceHost), port);
        public static AspNetCoreServirtiumServer WithCommandLineArgs(string[] args, IInteractionMonitor monitor, IInteractionTransforms interactionTransforms) =>
            new AspNetCoreServirtiumServer(Host.CreateDefaultBuilder(args), monitor, interactionTransforms, null);

        //private static readonly 
        internal AspNetCoreServirtiumServer(IHostBuilder hostBuilder, IInteractionMonitor monitor, IInteractionTransforms interactionTransforms, int? port)
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
                    var handler = new AspNetCoreServirtiumRequestHandler(_interactionMonitor, interactionTransforms, _interactionCounter);
                    app.Run(async ctx =>
                    {
                        var targetHost = new Uri($"{ctx.Request.Scheme}{Uri.SchemeDelimiter}{ctx.Request.Host}");
                        var pathAndQuery = $"{ctx.Request.Path}{ctx.Request.QueryString}";

                        ctx.Response.OnCompleted(() =>
                        {
                            Console.WriteLine($"{ctx.Request.Method} Request to {targetHost}{pathAndQuery} returned to client with code {ctx.Response.StatusCode}");
                            return Task.CompletedTask;
                        });
                        List<IInteraction.Note> notes;
                        lock (_notesForNextInteraction)
                        {
                            notes = new List<IInteraction.Note>(_notesForNextInteraction);
                            _notesForNextInteraction.Clear();
                        }
                        await handler.HandleRequest(targetHost, pathAndQuery, ctx.Request.Method, ctx.Request.Headers, ctx.Request.ContentType, ctx.Request.Body, (code) => ctx.Response.StatusCode = (int)code, ctx.Response.Headers, ctx.Response.Body, notes);

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

        public void MakeNote(string title, string note)
        {
            _notesForNextInteraction.Add(new IInteraction.Note(IInteraction.Note.NoteType.Text, title, note));
        }

        public void MakeCodeNote(string title, string code)
        {
            _notesForNextInteraction.Add(new IInteraction.Note(IInteraction.Note.NoteType.Code, title, code));
        }
    }
}
