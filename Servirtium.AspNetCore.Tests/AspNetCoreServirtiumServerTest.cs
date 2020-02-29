using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Moq;
using Servirtium.Core;
using System;
using Xunit;

namespace Servirtium.AspNetCore.Tests
{
    public class AspNetCoreServirtiumServerTest
    {
        private readonly Mock<IHostBuilder> _mockBuilder;

        private readonly Mock<IHost> _mockHost;

        public AspNetCoreServirtiumServerTest()
        {
            _mockHost = new Mock<IHost>();

            _mockBuilder = new Mock<IHostBuilder>();
            _mockBuilder.Setup(b => b.Build()).Returns(_mockHost.Object);
        }

        [Fact]
        public void Constructor_Always_ConfiguresAndBuildsHost()
        {
            new AspNetCoreServirtiumServer(
                _mockBuilder.Object, 
                new Mock<IServirtiumRequestHandler>().Object,
                null
            );
            _mockBuilder.Verify(b => b.Build());
        }
    }
}
