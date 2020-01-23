using System;
using Xunit;

namespace Servirtium.Demo
{
    public class ClimateApiDirectTests : ClimateApiTests
    {

        internal override ClimateApi ClimateApi { get; } = new ClimateApi();
    }
}
