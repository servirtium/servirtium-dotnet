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

namespace Servirtium.AspNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AspNetCoreServirtiumServer.WithCommandLineArgs(args, new PassThroughInteractionMonitor(new Uri("http://climatedataapi.worldbank.org")), new SimpleInteractionTransforms(new Uri("http://climatedataapi.worldbank.org"), new Regex[0], new Regex[0])).Start().Wait();
            Console.ReadKey();
        }

    }
}
