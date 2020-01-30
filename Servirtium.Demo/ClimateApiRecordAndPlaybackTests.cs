using Servirtium.AspNetCore;
using Servirtium.Core;
using Servirtium.Core.Record;
using Servirtium.Core.Replay;
using System;
using System.Collections.Generic;
using System.Text;

namespace Servirtium.Demo
{
    [Xunit.Collection("Servirtium Demo")]
    public class ClimateApiRecordAndPlaybackTests : ClimateApiTests
    {
        internal override IEnumerable<(IServirtiumServer, ClimateApi)> GenerateTestServerClientPairs(string script)
        {
            var recorder = new MarkdownRecorder(ClimateApi.DEFAULT_SITE, $@"..\..\..\test_recording_output\{script}");
            yield return 
            (
                AspNetCoreServirtiumServer.Default(recorder, ClimateApi.DEFAULT_SITE),
                new ClimateApi(new Uri("http://localhost:5000"))
            ); 
            var replayer = new MarkdownReplayer();
            replayer.LoadScriptFile($@"..\..\..\test_recording_output\{script}");
            yield return
            (
                AspNetCoreServirtiumServer.Default(replayer, ClimateApi.DEFAULT_SITE),
                new ClimateApi(new Uri("http://localhost:5000"))
            );
        }
    }
}
