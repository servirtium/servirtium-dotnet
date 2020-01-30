using Servirtium.AspNetCore;
using Servirtium.Core;
using Servirtium.Core.Record;
using Servirtium.Core.Replay;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static Servirtium.Core.FindAndReplaceScriptWriter;

namespace Servirtium.Demo
{
    [Xunit.Collection("Servirtium Demo")]
    public class ClimateApiRecordAndPlaybackTests : ClimateApiTests
    {
        internal override IEnumerable<(IServirtiumServer, ClimateApi)> GenerateTestServerClientPairs(string script)
        {
            var recorder = new MarkdownRecorder(
                ClimateApi.DEFAULT_SITE, $@"..\..\..\test_recording_output\{script}",
                new ServiceInteropViaSystemNetHttp(),
                new FindAndReplaceScriptWriter(new[] {
                    new RegexReplacement(new Regex("Set-Cookie: AWSALB=.*"), "Set-Cookie: AWSALB=REPLACED-IN-RECORDING; Expires=Thu, 15 Jan 2099 11:11:11 GMT; Path=/"),
                    new RegexReplacement(new Regex("Set-Cookie: TS0137860d=.*"), "Set-Cookie: TS0137860d=ALSO-REPLACED-IN-RECORDING; Path=/"),
                    new RegexReplacement(new Regex("Set-Cookie: TS01c35ec3=.*"), "Set-Cookie: TS01c35ec3=ONE-MORE-REPLACED-IN-RECORDING; Path=/"),
                    new RegexReplacement(new Regex("Set-Cookie: climatedataapi.cookie=.*"), "Set-Cookie: climatedataapi.cookie=1234567899999; Path=/"),
                    new RegexReplacement(new Regex("Set-Cookie: climatedataapi_ext.cookie=.*"), "Set-Cookie: climatedataapi_ext.cookie=9876543211111; Path=/"),
                    new RegexReplacement(new Regex("User-Agent: .*"), "User-Agent: Servirtium-Testing")
                }, new MarkdownScriptWriter()));
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
