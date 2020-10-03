using System;
using System.IO;
using System.Text.RegularExpressions;
using Servirtium.AspNetCore;
using Servirtium.Core;
using Servirtium.Core.Http;
using Servirtium.Core.Interactions;

namespace Servirtium.StandaloneServer
{
    public class Program
    {
        internal static readonly string RECORDING_OUTPUT_DIRECTORY = @"..\..\..\test_recording_output".Replace('\\', Path.DirectorySeparatorChar);

        public static void Main(string[] args)
        {
            var command = args[0].ToLower();
            IInteractionMonitor monitor = (command) switch
            {
                "playback" => new InteractionReplayer(),
                "record" => new InteractionRecorder(
                    Path.Combine(RECORDING_OUTPUT_DIRECTORY, "recording.md"),
                    new FindAndReplaceScriptWriter(new[] { new FindAndReplaceScriptWriter.RegexReplacement(new Regex("User-Agent: .*"), "User-Agent: Servirtium-Testing") }, 
                    new MarkdownScriptWriter())
                ),
                _ => throw new ArgumentException($"Unsupported command: '{command}', supported commands are 'playback' and 'record'."),
            };
            AspNetCoreServirtiumServer.WithCommandLineArgs(args, monitor, new SimpleHttpMessageTransforms(
                new Uri("http://todo-backend-sinatra.herokuapp.com"),
                new Regex[0],
                new[] { new Regex("Date:") })).Start().Wait();
            Console.ReadKey();
        }

    }
}