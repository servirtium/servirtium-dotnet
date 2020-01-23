using Servirtium.AspNetCore;
using Servirtium.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Servirtium.Demo
{
    public class ClimateApiPassThroughTests : ClimateApiTests, IDisposable
    {
        IServirtiumServer _server = AspNetCoreServirtiumServer.Default(new PassThroughInteractionMonitor(new Uri(ClimateApi.DEFAULT_SITE)));

        public ClimateApiPassThroughTests()
        {
            _server.Start();
        }

        internal override ClimateApi ClimateApi => new ClimateApi("http://localhost:5000");

        public void Dispose()
        {
            _server.Stop();
        }
    }
}
