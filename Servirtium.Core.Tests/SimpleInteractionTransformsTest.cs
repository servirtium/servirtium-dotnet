using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using static Servirtium.Core.SimpleInteractionTransforms;

namespace Servirtium.Core.Tests
{
    public class SimpleInteractionTransformsTest
    {

        private static readonly Uri REAL_SERVICE_URI = new Uri("http://real-service.com");

        private static readonly Uri SERVIRTIUM_SERVICE_URI = new Uri("http://servirtium-service.com");

        IInteraction _baseInteraction = new ImmutableInteraction.Builder()
            .Number(1337)
            .Path("down/the/garden")
            .RequestHeaders(new[] { ("mock-header", "mock-value"), ("another-mock-header", "another-mock-value") })
            .ResponseHeaders(new[] { ("mock-response-header", "mock-value"), ("another-mock-response-header", "another-mock-value") })
            .Build();

        ServiceResponse _baseResponse = new ServiceResponse(null, null, System.Net.HttpStatusCode.OK, new[] { ("mock-response-header", "mock-value"), ("another-mock-response-header", "another-mock-value") });

        [Fact]
        public void TransformClientRequestForRealService_NoRequestHeadersToRemoveAndNoHostRequestHeader_ReturnsUnchangedClone()
        {
            var transformed =new SimpleInteractionTransforms(REAL_SERVICE_URI)
                .TransformClientRequestForRealService(_baseInteraction);
            Assert.Equal(_baseInteraction.Number, transformed.Number);
            Assert.Equal(_baseInteraction.Path, transformed.Path);
            Assert.Equal(_baseInteraction.RequestHeaders, transformed.RequestHeaders);
            Assert.Null(transformed.RequestContentType);
            Assert.Null(transformed.RequestBody);
            Assert.Equal(_baseInteraction.ResponseHeaders, transformed.ResponseHeaders.ToArray());
        }

        [Fact]
        public void TransformClientRequestForRealService_RequestHeadersToRemoveNotMatchingAndNoHostRequestHeader_ReturnsUnchangedClone()
        {
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI, new Regex[] { new Regex("missing-mock-header"), new Regex("another-missing-mock-header") }, new Regex[0])
                .TransformClientRequestForRealService(_baseInteraction);
            Assert.Equal(_baseInteraction.Number, transformed.Number);
            Assert.Equal(_baseInteraction.Path, transformed.Path);
            Assert.Equal(_baseInteraction.RequestHeaders, transformed.RequestHeaders);
            Assert.Null(transformed.RequestContentType);
            Assert.Null(transformed.RequestBody);
            Assert.Equal(_baseInteraction.ResponseHeaders, transformed.ResponseHeaders.ToArray());
        }

        [Fact]
        public void TransformClientRequestForRealService_RequestHeaderExcludeOnlyMatchesName_ExcludesNothing()
        {
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI, new Regex[] { new Regex(@"^mock-header$") }, new Regex[0])
                .TransformClientRequestForRealService(_baseInteraction);
            Assert.Equal(_baseInteraction.RequestHeaders, transformed.RequestHeaders.ToArray());
        }

        [Fact]
        public void TransformClientRequestForRealService_RequestHeaderExcludeOnlyMatchesValue_ExcludesNothing()
        {
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI, new Regex[] { new Regex(@"^mock-value$") }, new Regex[0])
                .TransformClientRequestForRealService(_baseInteraction);
            Assert.Equal(_baseInteraction.RequestHeaders, transformed.RequestHeaders.ToArray());
        }

        [Fact]
        public void TransformClientRequestForRealService_RequestHeaderExcludeMatchesColonSeparatedNameAndValue_ExcludesMatchingHeader()
        {
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI, new Regex[] { new Regex(@"mock-header:\s*another") }, new Regex[0])
                .TransformClientRequestForRealService(_baseInteraction);
            Assert.Equal(new[] { ("mock-header", "mock-value") }, transformed.RequestHeaders.ToArray());
        }

        [Fact]
        public void TransformClientRequestForRealService_RequestHeaderExcludeOnlyMatchesResponseHeader_ExcludesNothing()
        {
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI, new Regex[] { new Regex(@"mock-response-header") }, new Regex[0])
                .TransformClientRequestForRealService(_baseInteraction);
            Assert.Equal(_baseInteraction.RequestHeaders, transformed.RequestHeaders.ToArray());
            Assert.Equal(_baseInteraction.ResponseHeaders, transformed.ResponseHeaders.ToArray());
        }

        [Fact]
        public void TransformClientRequestForRealService_SingleRequestHeaderExcludeMatchesMultipleValues_ExcludesAllMatchingHeaders()
        {
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI, new Regex[] { new Regex(@"mock-header") }, new Regex[0])
                .TransformClientRequestForRealService(_baseInteraction);
            Assert.Empty(transformed.RequestHeaders);
        }

        [Fact]
        public void TransformClientRequestForRealService_MultipleRequestHeaderExcludeMatchDifferentValues_ExcludesAllMatchingHeaders()
        {
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI, new Regex[] { new Regex(@"^mock-header"), new Regex(@"^another-mock-header") }, new Regex[0])
                .TransformClientRequestForRealService(_baseInteraction);
            Assert.Empty(transformed.RequestHeaders);
        }

        [Fact]
        public void TransformClientRequestForRealService_HostHeaderInRequest_ReplacesHostHeaderValueWithRealServiceHost()
        {
            var interaction = new ImmutableInteraction.Builder()
                .From(_baseInteraction)
                .RequestHeaders(new[] { ("Host", SERVIRTIUM_SERVICE_URI.Host) })
                .Build();
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI).TransformClientRequestForRealService(interaction);
            Assert.Equal(("Host", REAL_SERVICE_URI.Host), transformed.RequestHeaders.First());
        }

        [Fact]
        public void TransformClientRequestForRealService_HostHeaderInRequest_RetainsCasingOfHostHeader()
        {
            var interaction = new ImmutableInteraction.Builder()
                .From(_baseInteraction)
                .RequestHeaders(new[] { ("host", SERVIRTIUM_SERVICE_URI.Host) })
                .Build();
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI).TransformClientRequestForRealService(interaction);
            Assert.Equal(("host", REAL_SERVICE_URI.Host), transformed.RequestHeaders.First());
            interaction = new ImmutableInteraction.Builder()
                .From(_baseInteraction)
                .RequestHeaders(new[] { ("hOsT", SERVIRTIUM_SERVICE_URI.Host) })
                .Build();
            transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI).TransformClientRequestForRealService(interaction);
            Assert.Equal(("hOsT", REAL_SERVICE_URI.Host), transformed.RequestHeaders.First());
        }

        [Fact]
        public void TransformClientRequestForRealService_HostHeaderInRequestAndHostMatchesRequestExcludePattern_RemovesHostHeader()
        {
            var interaction = new ImmutableInteraction.Builder()
                .From(_baseInteraction)
                .RequestHeaders(new[] { ("Host", SERVIRTIUM_SERVICE_URI.Host), ("mock-header", "mock-value") })
                .Build();
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI, new[] { new Regex(@"Host") }, new Regex[0]).TransformClientRequestForRealService(interaction);
            Assert.Equal(new[] { ("mock-header", "mock-value") }, transformed.RequestHeaders);
        }

        [Fact]
        public void TransformRealServiceResponseForClient_NoRequestHeadersToRemove_ReturnsUnchangedClone()
        {
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI)
                .TransformRealServiceResponseForClient(_baseResponse);
            Assert.Equal(_baseResponse.Headers, transformed.Headers);
            Assert.Null(transformed.ContentType);
            Assert.Null(transformed.Body);
        }

        [Fact]
        public void TransformRealServiceResponseForClient_RequestHeadersToRemoveNotMatchingAndNoHostRequestHeader_ReturnsUnchangedClone()
        {
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI, new Regex[0], new Regex[] { new Regex("missing-mock-header"), new Regex("another-missing-mock-header") })
                .TransformRealServiceResponseForClient(_baseResponse);
            Assert.Equal(_baseResponse.Headers, transformed.Headers);
            Assert.Null(transformed.ContentType);
            Assert.Null(transformed.Body);
        }

        [Fact]
        public void TransformRealServiceResponseForClient_ResponseHeaderExcludeOnlyMatchesName_ExcludesNothing()
        {
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI, new Regex[0], new Regex[] { new Regex(@"^mock-response-header$") })
                .TransformRealServiceResponseForClient(_baseResponse);
            Assert.Equal(_baseResponse.Headers, transformed.Headers.ToArray());
        }

        [Fact]
        public void TransformRealServiceResponseForClient_ResponseHeaderExcludeOnlyMatchesValue_ExcludesNothing()
        {
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI, new Regex[0], new Regex[] { new Regex(@"^mock-value$") })
                .TransformRealServiceResponseForClient(_baseResponse);
            Assert.Equal(_baseResponse.Headers, transformed.Headers.ToArray());
        }

        [Fact]
        public void TransformRealServiceResponseForClient_ResponseHeaderExcludeMatchesColonSeparatedNameAndValue_ExcludesMatchingHeader()
        {
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI, new Regex[0], new Regex[] { new Regex(@"mock-response-header:\s*another") })
                .TransformRealServiceResponseForClient(_baseResponse);
            Assert.Equal(new[] { ("mock-response-header", "mock-value") }, transformed.Headers.ToArray());
        }


        [Fact]
        public void TransformRealServiceResponseForClient_SingleRequestHeaderExcludeMatchesMultipleValues_ExcludesAllMatchingHeaders()
        {
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI, new Regex[0], new Regex[] { new Regex(@"mock-response-header") })
                .TransformRealServiceResponseForClient(_baseResponse);
            Assert.Empty(transformed.Headers);
        }

        [Fact]
        public void TransformRealServiceResponseForClient_MultipleRequestHeaderExcludeMatchDifferentValues_ExcludesAllMatchingHeaders()
        {
            var transformed = new SimpleInteractionTransforms(REAL_SERVICE_URI, new Regex[0], new Regex[] { new Regex(@"^mock-response-header"), new Regex(@"^another-mock-response-header") })
                .TransformRealServiceResponseForClient(_baseResponse);
            Assert.Empty(transformed.Headers);
        }
    }
}
