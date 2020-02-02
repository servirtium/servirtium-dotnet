using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace Servirtium.Core.Tests
{
    public class ServiceResponseTest
    {
        private static readonly MediaTypeHeaderValue TEST_MEDIA_TYPE = new MediaTypeHeaderValue("application/json");
        private static IEnumerable<(string, string)> generateTestHeaders()=> new [] { 
            ("first", "the"),
            ("second", "original"),
            ("third", "headers")
        };

        [Fact]
        public void WithRevisedHeaders_ValidHeaders_ReturnsServiceResponseWithNewHeadersAndOtherPropertiesTheSame()
        {
            var revised = new ServiceResponse("The body.", TEST_MEDIA_TYPE, HttpStatusCode.Ambiguous, generateTestHeaders())
                .WithRevisedHeaders(new[] {
                    ("first", "some"),
                    ("second", "new"),
                    ("third", "headers")
                });
            Assert.Equal("The body.", revised.Body);
            Assert.Equal(TEST_MEDIA_TYPE, revised.ContentType);
            Assert.Equal(HttpStatusCode.Ambiguous, revised.StatusCode);
            Assert.Equal(new[] {
                    ("first", "some"),
                    ("second", "new"),
                    ("third", "headers")
                }, revised.Headers);
        }

        [Fact]
        public void WithRevisedBody_ValidBodyAndNoContentLengthHeader_ReturnsServiceResponseWithNewBodyAndOtherPropertiesTheSame()
        {
            var revised = new ServiceResponse("The body.", TEST_MEDIA_TYPE, HttpStatusCode.Ambiguous, generateTestHeaders())
                .WithRevisedBody("The new body.");
            Assert.Equal("The new body.", revised.Body);
            Assert.Equal(TEST_MEDIA_TYPE, revised.ContentType);
            Assert.Equal(HttpStatusCode.Ambiguous, revised.StatusCode);
            Assert.Equal(generateTestHeaders(), revised.Headers);
        }

        [Fact]
        public void WithRevisedBody_ValidBodyAndCapitalisedContentLengthHeader_ReturnsServiceResponseWithNewBodyAndContentLengthSetToBodyLength()
        {
            var revised = new ServiceResponse("The body.", TEST_MEDIA_TYPE, HttpStatusCode.Ambiguous, new[]{
                ("first", "the"),
                ("second", "original"),
                ("Content-Length", "A gazillion"),
                ("third", "headers")})
                .WithRevisedBody("The new body.");
            Assert.Equal("The new body.", revised.Body);
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

            var revised = new ServiceResponse("The body.", TEST_MEDIA_TYPE, HttpStatusCode.Ambiguous, new[]{ 
                ("first", "the"),
                ("second", "original"),
                ("content-length", "A gazillion"),
                ("third", "headers")
            })
                .WithRevisedBody("The new body.");
            Assert.Equal("The new body.", revised.Body);
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

            var revised = new ServiceResponse("The body.", TEST_MEDIA_TYPE, HttpStatusCode.Ambiguous, new[]{
                ("first", "the"),
                ("second", "original"),
                ("CONTENT-LENGTH", "A gazillion"),
                ("third", "headers")
            })
                .WithRevisedBody("The new body.");
            Assert.Equal("The new body.", revised.Body);
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
