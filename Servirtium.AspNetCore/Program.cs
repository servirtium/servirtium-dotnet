using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Servirtium.Core;
using Servirtium.Core.Replay;

namespace Servirtium.AspNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var replayer = new MarkdownReplayer();
            replayer.LoadScriptFile($@"..\Servirtium.Demo\test_playbacks\averageRainfallForGreatBritainFrom1980to1999Exists.md");

            AspNetCoreServirtiumServer.WithCommandLineArgs(args, replayer, new SimpleInteractionTransforms(
                new Uri("http://climatedataapi.worldbank.org"),
                new Regex[0],
                new[] { new Regex("Date:") })).Start().Wait();
            Console.ReadKey();
        }

    }
}
