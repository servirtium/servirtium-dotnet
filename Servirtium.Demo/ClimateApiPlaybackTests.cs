using Servirtium.AspNetCore;
using Servirtium.Core;
using Servirtium.Core.Replay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Servirtium.Demo
{
    [Xunit.Collection("Servirtium Demo")]
    public class ClimateApiPlaybackTests : ClimateApiTests
    {
        internal override IEnumerable<(IServirtiumServer, ClimateApi)> GenerateTestServerClientPairs(string script)
        {
            var replayer = new MarkdownReplayer();
            replayer.LoadScriptFile($@"..\..\..\test_playbacks\{script}");
            yield return
            (
                AspNetCoreServirtiumServer.WithTransforms(
                    1234,
                    replayer, 
                    new SimpleInteractionTransforms(
                        ClimateApi.DEFAULT_SITE, 
                        new Regex[0], 
                        new[] { new Regex("Date:") }

                    )),
                new ClimateApi(new Uri("http://localhost:1234"))
            );
        }
    }
}
