using Moq;
using Servirtium.Core.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace Servirtium.Core.Tests.Http
{
    public class ServiceRequestTest
    {
        private static readonly MediaTypeHeaderValue TEST_MEDIA_TYPE = new MediaTypeHeaderValue("application/json");

        private static IEnumerable<(string, string)> generateTestHeaders()=> new [] { 
            ("first", "the"),
            ("second", "original"),
            ("third", "headers")
        };

        private static ServiceRequest BaselineResponse() => new ServiceRequest.Builder()
            .Url(new Uri("http://a.service/and/path"))
            .Body("The body.", TEST_MEDIA_TYPE)
            .Method(HttpMethod.Delete)
            .Headers(generateTestHeaders())
            .Build();


        private static string BodyAsString(byte[]? body)=> Encoding.UTF8.GetString(body!);


        [Fact]
        public void Headers_ValidHeaders_ReturnsServiceRequestWithNewHeadersAndOtherPropertiesTheSame()
        {
            var revised = new ServiceRequest.Builder()
                .From(BaselineResponse())
                .Headers(new[] {
                    ("first", "some"),
                    ("second", "new"),
                    ("third", "headers")
                })
                .Build();
            Assert.Equal("The body.", BodyAsString(revised.Body));
            Assert.Equal(TEST_MEDIA_TYPE, revised.ContentType);
            Assert.Equal(HttpMethod.Delete, revised.Method);
            Assert.Equal(new[] {
                    ("first", "some"),
                    ("second", "new"),
                    ("third", "headers")
                }, revised.Headers);
        }

        [Fact]
        public void Body_ValidBody_ReturnsServiceRequestWithNewBodyAndOtherPropertiesTheSame()
        {
            var revised = new ServiceRequest.Builder()
                .From(BaselineResponse())
                .Body("The new body.", MediaTypeHeaderValue.Parse("text/plain"))
                .Build();
            Assert.Equal("The new body.", BodyAsString(revised.Body));
            Assert.Equal(MediaTypeHeaderValue.Parse("text/plain"), revised.ContentType);
            Assert.Equal(HttpMethod.Delete, revised.Method);
            Assert.Equal(generateTestHeaders(), revised.Headers);
        }

        [Fact]
        public void Body_ValidBinaryBody_ReturnsServiceRequestWithNewBodyAndOtherPropertiesTheSame()
        {
            var revised = new ServiceRequest.Builder()
                .From(BaselineResponse())
                .Body(Encoding.UTF8.GetBytes("The new body."), MediaTypeHeaderValue.Parse("text/plain"))
                .Build();
            Assert.Equal("The new body.", BodyAsString(revised.Body));
            Assert.Equal(MediaTypeHeaderValue.Parse("text/plain"), revised.ContentType);
            Assert.Equal(HttpMethod.Delete, revised.Method);
            Assert.Equal(generateTestHeaders(), revised.Headers);
        }

        [Fact]
        public void Body_HasHeaders_FixesContentLengthHeaders()
        {
            var originalHeaders = new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("Content-Length", "A gazillion"),
                    ("third", "headers")};
            var fixedHeaders = new[] { ("fixed", "headers") };
            var mockHeaderFixer =
                new Mock<Func<IEnumerable<(string, string)>, byte[], bool, IEnumerable<(string, string)>>>();

            mockHeaderFixer.Setup(hf => hf(It.IsAny<IEnumerable<(string, string)>>(), It.IsAny<byte[]>(), It.IsAny<bool>()))
                .Returns(fixedHeaders);

            var revised = new ServiceRequest.Builder(mockHeaderFixer.Object)
                .From(BaselineResponse())
                .Headers(originalHeaders)
                .Body("The new body.", TEST_MEDIA_TYPE)
                .Build();
            mockHeaderFixer.Verify(hf => hf(originalHeaders, It.Is<byte[]>(b=>Encoding.UTF8.GetString(b)=="The new body."), false));
            Assert.Equal(fixedHeaders, revised.Headers);
        }

        [Fact]
        public void Body_CreateContentLengthHeaderFlagSet_PassesFlagToHeaderFixFunc()
        {
            var originalHeaders = new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("Content-Length", "A gazillion"),
                    ("third", "headers")};
            var fixedHeaders = new[] { ("fixed", "headers") };
            var mockHeaderFixer =
                new Mock<Func<IEnumerable<(string, string)>, byte[], bool, IEnumerable<(string, string)>>>();

            mockHeaderFixer.Setup(hf => hf(It.IsAny<IEnumerable<(string, string)>>(), It.IsAny<byte[]>(), It.IsAny<bool>()))
                .Returns(fixedHeaders);

            var revised = new ServiceRequest.Builder(mockHeaderFixer.Object)
                .From(BaselineResponse())
                .Headers(originalHeaders)
                .Body("The new body.", TEST_MEDIA_TYPE, true)
                .Build();
            mockHeaderFixer.Verify(hf => hf(originalHeaders, It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == "The new body."), true));
        }


        [Fact]
        public void From_FullyPopulatedResposse_CorrectlyCopiesAllProperties()
        {
            var original = BaselineResponse();
            var revised = new ServiceRequest.Builder()
                .From(original)
                .Build();
            Assert.Equal(original.Url, revised.Url);
            Assert.Equal(original.Method, revised.Method);
            Assert.Equal(original.Body, revised.Body);
            Assert.Equal(original.Headers, revised.Headers);
        }
    }
}
