using System;
using System.Text.RegularExpressions;
using Servirtium.AspNetCore;
using Servirtium.Core;
using Servirtium.Core.Http;
using Servirtium.Core.Interactions;

namespace CompatibilitySuite
{
  internal class Program
  {
    public static void Main(string[] args)
    {
      if (args[1].Equals("playback"))
      {
        var replayer = new InteractionReplayer();
        replayer.LoadScriptFile($@"\todobackend-mocha-suite-interactions.md");

        AspNetCoreServirtiumServer.WithCommandLineArgs(args, replayer, new SimpleHttpMessageTransforms(
          new Uri("https://todo-backend-sinatra.herokuapp.com/todos"),
        Console.ReadKey();
      } else if (args[1].Equals("record"))
      {
        // TODO
      }
    }
  }
}