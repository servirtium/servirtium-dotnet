using Servirtium.AspNetCore;
using Servirtium.Core;
using Servirtium.Core.Record;
using Servirtium.Core.Replay;
using System;
using System.Collections.Generic;
using System.Linq;
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
                AspNetCoreServirtiumServer.WithTransforms(
                    1234,
                    recorder,
                    new SimpleInteractionTransforms(
                        ClimateApi.DEFAULT_SITE,
                        new Regex[0],
                        new[] {
                        "Date:", "X-", "Strict-Transport-Security",
                        "Content-Security-Policy", "Cache-Control", "Secure", "HttpOnly",
                        "Set-Cookie: climatedata.cookie=" }.Select(pattern => new Regex(pattern))
                    )),
                new ClimateApi(new Uri("http://localhost:1234"))
            ); 
            var replayer = new MarkdownReplayer();
            replayer.LoadScriptFile($@"..\..\..\test_recording_output\{script}");
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
