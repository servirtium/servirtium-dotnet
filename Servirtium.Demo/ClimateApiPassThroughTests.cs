using Servirtium.AspNetCore;
using Servirtium.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Servirtium.Demo
{
    [Xunit.Collection("Servirtium Demo")]
    public class ClimateApiPassThroughTests : ClimateApiTests, IDisposable
    {

        internal override ClimateApi ClimateApi => new ClimateApi(new Uri("http://localhost:5000"));

        internal override Func<string, IServirtiumServer> MonitorFactory => (script)=>AspNetCoreServirtiumServer.Default(new PassThroughInteractionMonitor(ClimateApi.DEFAULT_SITE), ClimateApi.DEFAULT_SITE);

    }
}
