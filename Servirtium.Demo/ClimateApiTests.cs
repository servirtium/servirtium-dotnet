using Servirtium.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Servirtium.Demo
{
    public abstract class ClimateApiTests : IDisposable
    {

        private IServirtiumServer _server = new StubServirtiumServer();
        internal abstract Func<string, IServirtiumServer> MonitorFactory { get; }
        internal abstract ClimateApi ClimateApi { get; }

        [Fact]
        public virtual void AverageRainfallForGreatBritainFrom1980to1999Exists()
        {
            _server = MonitorFactory("test_playbacks/averageRainfallForGreatBritainFrom1980to1999Exists.md");
            _server.Start();
            Assert.Equal(988.8454972331015, ClimateApi.getAveAnnualRainfall(1980, 1999, "gbr").Result, 0);
        }

        [Fact]
        public void AverageRainfallForFranceFrom1980to1999Exists()
        {
            _server = MonitorFactory("test_playbacks/averageRainfallForFranceFrom1980to1999Exists.md");
            _server.Start();
            Assert.Equal(913.7986955122727, ClimateApi.getAveAnnualRainfall(1980, 1999, "fra").Result, 0);
        }

        [Fact]
        public void AverageRainfallForEgyptFrom1980to1999Exists()
        {
            _server = MonitorFactory("test_playbacks/averageRainfallForEgyptFrom1980to1999Exists.md");
            _server.Start();
            Assert.Equal(54.58587712129825, ClimateApi.getAveAnnualRainfall(1980, 1999, "egy").Result, 0);
        }

        [Fact]
        public void AverageRainfallForGreatBritainFrom1985to1995DoesNotExist()
        {
            _server = MonitorFactory("test_playbacks/averageRainfallForGreatBritainFrom1985to1995DoesNotExist.md");
            _server.Start();
            var e = Assert.Throws<AggregateException>(() => ClimateApi.getAveAnnualRainfall(1985, 1995, "gbr").Wait());
            Assert.Equal("date range 1985-1995 not supported", e.InnerExceptions[0].Message);
        }

        [Fact]
        public void AverageRainfallForMiddleEarthFrom1980to1999DoesNotExist()
        {
            _server = MonitorFactory("test_playbacks/averageRainfallForMiddleEarthFrom1980to1999DoesNotExist.md");
            _server.Start();
            var e = Assert.Throws<AggregateException>(() => ClimateApi.getAveAnnualRainfall(1980, 1999, "mde").Wait());
            Assert.Equal("mde not recognized by climateweb", e.InnerExceptions[0].Message);
        }

        [Fact]
        public void AverageRainfallForGreatBritainAndFranceFrom1980to1999CanBeCalculatedFromTwoRequests()
        {
            _server = MonitorFactory("test_playbacks/averageRainfallForGreatBritainAndFranceFrom1980to1999CanBeCalculatedFromTwoRequests.md");
            _server.Start();
            Assert.Equal(951.3220963726872, ClimateApi.getAveAnnualRainfall(1980, 1999, "gbr", "fra").Result, 0);
        }
        public void Dispose()
        {
            _server?.Stop().Wait();
        }
    }
}
