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
            var command = args[0].ToLower();
            var sourceUrl = args[1];
            var scriptDirectory = Directory.CreateDirectory(RECORDING_OUTPUT_DIRECTORY);
            var loggerFactory= LoggerFactory.Create((builder) =>
            {
                builder.SetMinimumLevel(LogLevel.Debug)
                    .AddFilter(level => true)
                    .AddConsole();
            });
            IInteractionMonitor monitor;
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
                            Path.Combine(RECORDING_OUTPUT_DIRECTORY, $"recording_{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.md"),
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
                args, 
                monitor, 
                new HttpMessageTransformPipeline(
                    new SimpleHttpMessageTransforms(
                        new Uri(sourceUrl),
                        new Regex[] { },
                        new[] { new Regex("Date:") },
                        loggerFactory
                    ),
                    new FindAndReplaceHttpMessageTransforms(
                        new[] {
                            new RegexReplacement(new Regex(Regex.Escape(sourceUrl)), args[2], ReplacementContext.ResponseBody)
                        }, loggerFactory)
                ),
                loggerFactory
            );
            server.Start().Wait();
            AppDomain.CurrentDomain.ProcessExit += (a, e) => server.Stop().Wait();
            Console.Read();

            server.Stop().Wait();
        }

    }
}