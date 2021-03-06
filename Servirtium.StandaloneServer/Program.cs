using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Servirtium.AspNetCore;
using Servirtium.Core;
using Servirtium.Core.Http;
using Servirtium.Core.Interactions;

namespace Servirtium.StandaloneServer
{
    public class Program
    {
        internal static readonly string RECORDING_OUTPUT_DIRECTORY = @".\test_recording_output".Replace('\\', Path.DirectorySeparatorChar);

        public static void Main(string[] args)
        {
            var port = args.Length > 2 ? ushort.Parse(args[2]) : (ushort)61417;
            var command = args[0].ToLower();
            var sourceUrl = args[1];
            var scriptDirectory = Directory.CreateDirectory(RECORDING_OUTPUT_DIRECTORY);
            var loggerFactory= LoggerFactory.Create((builder) =>
            {
                builder.SetMinimumLevel(LogLevel.Debug)
                    .AddFilter(level => true)
                    .AddConsole();
            });
            var logger = loggerFactory.CreateLogger<Program>();
            IInteractionMonitor monitor;
            logger.LogInformation($"Initializing Servirtium Standalone Server in '{command}' mode");
            switch (command)
            {
                case "playback":
                    {
                        var replayer = new InteractionReplayer(null, null, null, null, loggerFactory); 
                        var scriptFile = scriptDirectory.GetFiles()
                                     .OrderByDescending(f => f.LastWriteTime)
                                     .First();
                        replayer.LoadScriptFile(scriptFile.FullName);
                        monitor = replayer;
                        break;
                    }
                case "record":
                    {
                        monitor = new InteractionRecorder(
                            Path.Combine(RECORDING_OUTPUT_DIRECTORY, $"recording.md"),
                            new MarkdownScriptWriter(null, loggerFactory), 
                            false,
                            loggerFactory,
                            //Write each interaction after it completes rather than all at the end, so it isn't disrupted by unceremonious teardowns (like via a docker container stopping).
                            InteractionRecorder.RecordTime.AfterEachInteraction
                        );
                        break;
                    }
                default:
                    throw new ArgumentException($"Unsupported command: '{command}', supported commands are 'playback' and 'record'.");
            };
            var server = AspNetCoreServirtiumServer.WithCommandLineArgs(
                args.Append($"--urls=http://*:{port}").ToArray(), 
                monitor, 
                new HttpMessageTransformPipeline(
                    new SimpleHttpMessageTransforms(
                        new Uri(sourceUrl),
                        new Regex[] { },
                        new[] { new Regex("Date:"), new Regex("Via:"), 
                            new Regex("^X-"), new Regex("^X-"), new Regex("^Server:") },
                        loggerFactory
                    ),
                    new FindAndReplaceHttpMessageTransforms(
                        new[] {
                            new RegexReplacement(new Regex(Regex.Escape(sourceUrl)), $"http://localhost:{port}", ReplacementContext.ResponseBody)
                        }, loggerFactory)
                ),
                loggerFactory
            );
            logger.LogInformation("Starting up Servirtium Standalone Server.");

            //ProcessExit hook must be attached before server.Start() is called.
            //server.Start() attaches a standard ASP.NET ProcessExit hook and we need this one to run first,
            AppDomain.CurrentDomain.ProcessExit += (a, e) =>
            {
                logger.LogInformation("Servirtium Standalone Server attempting graceful shutdown.");
                server.Stop().Wait();
            };
            server.Start().Wait();
            logger.LogInformation("Servirtium Standalone Server started and listening.");
            while (Console.ReadLine()?.ToLower() != "exit") { }
            logger.LogDebug("Servirtium Standalone Server received 'exit' command, shutting down.");
            server.Stop().Wait();
        }

    }
}