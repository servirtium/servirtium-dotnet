using System;
using System.Text.RegularExpressions;
using Servirtium.AspNetCore;
using Servirtium.Core;

namespace Servirtium.StandaloneServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var replayer = new InteractionReplayer();
            replayer.LoadScriptFile($@"..\Servirtium.Demo\test_playbacks\averageRainfallForGreatBritainFrom1980to1999Exists.md");

            AspNetCoreServirtiumServer.WithCommandLineArgs(args, replayer, new SimpleInteractionTransforms(
                new Uri("http://climatedataapi.worldbank.org"),
                new Regex[0],
                new[] { new Regex("Date:") })).Start().Wait();
            Console.ReadKey();
        }

    }
}