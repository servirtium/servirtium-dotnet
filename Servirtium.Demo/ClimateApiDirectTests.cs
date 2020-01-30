using Servirtium.Core;
using System;
using System.Collections.Generic;

namespace Servirtium.Demo
{
    [Xunit.Collection("Servirtium Demo")]
    public class ClimateApiDirectTests : ClimateApiTests
    {
        internal override IEnumerable<(IServirtiumServer, ClimateApi)> GenerateTestServerClientPairs(string script)
        {
            yield return (new StubServirtiumServer(), new ClimateApi());
        }
    }
}
