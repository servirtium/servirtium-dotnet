using Servirtium.Core;
using System;

namespace Servirtium.Demo
{
    [Xunit.Collection("Servirtium Demo")]
    public class ClimateApiDirectTests : ClimateApiTests
    {

        internal override ClimateApi ClimateApi { get; } = new ClimateApi();

        internal override Func<string, IServirtiumServer> MonitorFactory => (script)=>new StubServirtiumServer();

    }
}
