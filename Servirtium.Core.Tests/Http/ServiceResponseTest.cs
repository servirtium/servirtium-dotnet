using Servirtium.Core.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace Servirtium.Core.Tests.Http
{
    public class ServiceResponseTest
    {
        private static readonly MediaTypeHeaderValue TEST_MEDIA_TYPE = new MediaTypeHeaderValue("application/json");

        private static IEnumerable<(string, string)> generateTestHeaders()=> new [] { 
            ("first", "the"),
            ("second", "original"),
            ("third", "headers")
        };
        private static ServiceResponse BaselineResponse() => new ServiceResponse.Builder()
            .Body("The body.", TEST_MEDIA_TYPE)
            .StatusCode(HttpStatusCode.Ambiguous)
            .Headers(new[] {
                ("first", "the"),
                ("second", "original"),
                ("third", "headers")
            })
            .Build();


        private static string BodyAsString(byte[]? body)=> Encoding.UTF8.GetString(body!);

        private static byte[] StringAsBody(string body) => Encoding.UTF8.GetBytes(body);


        [Fact]
        public void Headers_ValidHeaders_ReturnsServiceResponseWithNewHeadersAndOtherPropertiesTheSame()
        {
            var revised = new ServiceResponse.Builder()
                .From(BaselineResponse())
                .Headers(new[] {
                    ("first", "some"),
                    ("second", "new"),
                    ("third", "headers")
                })
                .Build();
            Assert.Equal("The body.", BodyAsString(revised.Body));
            Assert.Equal(TEST_MEDIA_TYPE, revised.ContentType);
            Assert.Equal(HttpStatusCode.Ambiguous, revised.StatusCode);
            Assert.Equal(new[] {
                    ("first", "some"),
                    ("second", "new"),
                    ("third", "headers")
                }, revised.Headers);
        }

        [Fact]
        public void Body_ValidBodyAndNoContentLengthHeader_ReturnsServiceResponseWithNewBodyAndOtherPropertiesTheSame()
        {
            var revised = new ServiceResponse.Builder()
                .From(BaselineResponse())
                .Body("The new body.", MediaTypeHeaderValue.Parse("text/plain"))
                .Build();
            Assert.Equal("The new body.", BodyAsString(revised.Body));
            Assert.Equal(MediaTypeHeaderValue.Parse("text/plain"), revised.ContentType);
            Assert.Equal(HttpStatusCode.Ambiguous, revised.StatusCode);
            Assert.Equal(generateTestHeaders(), revised.Headers);
        }

        [Fact]
        public void WithRevisedBody_ValidBodyAndCapitalisedContentLengthHeader_ReturnsServiceResponseWithNewBodyAndContentLengthSetToBodyLength()
        {
            var revised = new ServiceResponse.Builder()
                .From(BaselineResponse())
                .Headers(new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("Content-Length", "A gazillion"),
                    ("third", "headers")})
                .Body("The new body.", TEST_MEDIA_TYPE)
                .Build();
            Assert.Equal("The new body.", BodyAsString(revised.Body));
            Assert.Equal(TEST_MEDIA_TYPE, revised.ContentType);
            Assert.Equal(HttpStatusCode.Ambiguous, revised.StatusCode);
            Assert.Equal(
                new []{
                    ("first", "the"),
                    ("second", "original"),
                    ("Content-Length", "The new body.".Length.ToString()),
                    ("third", "headers")}
                , revised.Headers);
        }

        [Fact]
        public void WithRevisedBody_ValidBodyAndLowercaseContentLengthHeader_ReturnsServiceResponseWithNewBodyAndContentLengthSetToBodyLength()
        {
            var revised = new ServiceResponse.Builder()
                .From(BaselineResponse())
                .Headers(new[]{ 
                    ("first", "the"),
                    ("second", "original"),
                    ("content-length", "A gazillion"),
                    ("third", "headers")
                })
                .Body("The new body.", TEST_MEDIA_TYPE)
                .Build();
            Assert.Equal("The new body.", BodyAsString(revised.Body));
            Assert.Equal(TEST_MEDIA_TYPE, revised.ContentType);
            Assert.Equal(HttpStatusCode.Ambiguous, revised.StatusCode);
            Assert.Equal(
                new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("content-length", "The new body.".Length.ToString()),
                    ("third", "headers")}
                , revised.Headers);
        }

        [Fact]
        public void WithRevisedBody_ValidBodyAndContentLengthHeaderWithNonStandardCasing_ReturnsServiceResponseWithNewBodyAndContentLengthSetToBodyLength()
        {

            var revised = new ServiceResponse.Builder()
                .From(BaselineResponse())
                .Headers(new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("CONTENT-LENGTH", "A gazillion"),
                    ("third", "headers")
                })
                .Body("The new body.", TEST_MEDIA_TYPE)
                .Build();
            Assert.Equal("The new body.", BodyAsString(revised.Body));
            Assert.Equal(TEST_MEDIA_TYPE, revised.ContentType);
            Assert.Equal(HttpStatusCode.Ambiguous, revised.StatusCode);
            Assert.Equal(
                new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("CONTENT-LENGTH", "A gazillion"),
                    ("third", "headers")}
                , revised.Headers);
        }
    }
}
