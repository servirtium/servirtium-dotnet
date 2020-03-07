using Servirtium.Core.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace Servirtium.Core.Tests.Interactions
{
    public class ImmutableInteractionTest
    {
        [Fact]
        public void From_Always_CopiesAllPropertiesFromInputIntercationToBuilder()
        {
            var original = new ImmutableInteraction.Builder()
                .Path("/a/path")
                .Method(HttpMethod.Put)
                .RequestHeaders(new[] { ("a", "header") })
                .RequestBody("A body", MediaTypeHeaderValue.Parse("video/mp4"))
                .StatusCode(System.Net.HttpStatusCode.Created)
                .ResponseHeaders(new[] { ("moar", "headers") })
                .ResponseBody("Another body", MediaTypeHeaderValue.Parse("image/jpeg"))
                .Build();

            var copy = new ImmutableInteraction.Builder()
                .From(original)
                .Build();

            Assert.Equal(original.Path, copy.Path);
            Assert.Equal(original.Method, copy.Method);
            Assert.Equal(original.RequestHeaders.Single(), copy.RequestHeaders.Single());
            Assert.Equal(original.RequestBody, copy.RequestBody);
            Assert.Equal(original.RequestContentType, copy.RequestContentType);
            Assert.Equal(original.StatusCode, copy.StatusCode);
            Assert.Equal(original.ResponseHeaders.Single(), copy.ResponseHeaders.Single());
            Assert.Equal(original.ResponseBody, copy.ResponseBody);
            Assert.Equal(original.ResponseContentType, copy.ResponseContentType);
        }

        [Fact]
        public void RemoveRequestBody_Always_RemovesBodyAndContentType()
        {
            var original = new ImmutableInteraction.Builder()
                .Path("/a/path")
                .Method(HttpMethod.Put)
                .RequestHeaders(new[] { ("a", "header") })
                .RequestBody("A body", MediaTypeHeaderValue.Parse("video/mp4"))
                .Build();

            var bodyRemoved = new ImmutableInteraction.Builder()
                .From(original)
                .RemoveRequestBody()
                .Build();

            Assert.Equal(original.Path, bodyRemoved.Path);
            Assert.Equal(original.Method, bodyRemoved.Method);
            Assert.Null(bodyRemoved.RequestBody);
            Assert.Null(bodyRemoved.RequestContentType);
        }

        [Fact]
        public void RemoveResponseBody_Always_RemovesBodyAndContentType()
        {
            var original = new ImmutableInteraction.Builder()
                .StatusCode(System.Net.HttpStatusCode.Created)
                .ResponseBody("Another body", MediaTypeHeaderValue.Parse("image/jpeg"))
                .Build();

            var bodyRemoved = new ImmutableInteraction.Builder()
                .From(original)
                .RemoveResponseBody()
                .Build();
            Assert.Equal(original.StatusCode, bodyRemoved.StatusCode);
            Assert.Null(bodyRemoved.ResponseBody);
            Assert.Null(bodyRemoved.ResponseContentType);
        }
    }
}
