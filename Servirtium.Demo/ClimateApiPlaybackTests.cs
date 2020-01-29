using Servirtium.AspNetCore;
using Servirtium.Core;
using Servirtium.Core.Replay;
using System;
using System.Collections.Generic;
using System.Text;

namespace Servirtium.Demo
{
    [Xunit.Collection("Servirtium Demo")]
    public class ClimateApiPlaybackTests : ClimateApiTests
    {
        internal override ClimateApi ClimateApi => new ClimateApi(new Uri("http://localhost:5000"));

        internal override Func<string, IServirtiumServer> MonitorFactory => 
            (script) =>
            {
                var replayer = new MarkdownReplayer();
                replayer.LoadScriptFile($@"..\..\..\{script}");
                return AspNetCoreServirtiumServer.Default(replayer, ClimateApi.DEFAULT_SITE);
            };

    }
}
