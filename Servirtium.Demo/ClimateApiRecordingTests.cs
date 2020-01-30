using Servirtium.AspNetCore;
using Servirtium.Core;
using Servirtium.Core.Record;
using System;
using System.Collections.Generic;
using System.Text;

namespace Servirtium.Demo
{
    [Xunit.Collection("Servirtium Demo")]
    public class ClimateApiRecordingTests : ClimateApiTests
    {
        internal override IEnumerable<(IServirtiumServer, ClimateApi)> GenerateTestServerClientPairs(string script)
        {
            var recorder = new MarkdownRecorder(ClimateApi.DEFAULT_SITE, $@"..\..\..\test_recording_output\{script}");
            yield return 
            (
                AspNetCoreServirtiumServer.Default(recorder, ClimateApi.DEFAULT_SITE),
                new ClimateApi(new Uri("http://localhost:5000"))
            );
        }
    }
}
