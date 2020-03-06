using Servirtium.Core.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using static Servirtium.Core.Http.SimpleHttpMessageTransforms;

namespace Servirtium.Core.Tests.Http
{
    public class SimpleHttpMessageTransformsTest
    {

        private static readonly Uri REAL_SERVICE_URI = new Uri("http://real-service.com");

        private static readonly Uri SERVIRTIUM_SERVICE_URI = new Uri("http://servirtium-service.com");

        IRequestMessage _baseRequest = new ServiceRequest.Builder()
            .Url(new Uri(SERVIRTIUM_SERVICE_URI, "down/the/garden"))
            .Headers(new[] { ("mock-header", "mock-value"), ("another-mock-header", "another-mock-value") })
            .Build();

        IResponseMessage _baseResponse = new ServiceResponse.Builder()
            .StatusCode(System.Net.HttpStatusCode.OK)
            .Headers(("mock-response-header", "mock-value"), ("another-mock-response-header", "another-mock-value"))
            .Build();

        [Fact]
        public void TransformClientRequestForRealService_NoRequestHeadersToRemoveAndNoHostRequestHeader_ReturnsUnchangedClone()
        {
            var transformed =new SimpleHttpMessageTransforms(new Regex[0], new Regex[0])
                .TransformClientRequestForRealService(_baseRequest);
            Assert.Equal(new Uri("http://servirtium-service.com/down/the/garden"), transformed.Url);
            Assert.Equal(_baseRequest.Headers, transformed.Headers);
            Assert.Null(transformed.ContentType);
            Assert.Null(transformed.Body);
            Assert.Equal(_baseRequest.Headers, transformed.Headers.ToArray());
        }

        [Fact]
        public void TransformClientRequestForRealService_NoRequestHeadersToRemoveNoHostRequestHeaderAndServiceHostSet_ChangesUrlHost()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI)
                .TransformClientRequestForRealService(_baseRequest);
            Assert.Equal(new Uri("http://real-service.com/down/the/garden"), transformed.Url);
            Assert.Equal(_baseRequest.Headers, transformed.Headers);
            Assert.Null(transformed.ContentType);
            Assert.Null(transformed.Body);
            Assert.Equal(_baseRequest.Headers, transformed.Headers.ToArray());
        }

        [Fact]
        public void TransformClientRequestForRealService_RequestHeadersToRemoveNotMatchingAndNoHostRequestHeader_ReturnsUnchangedClone()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI, new Regex[] { new Regex("missing-mock-header"), new Regex("another-missing-mock-header") }, new Regex[0])
                .TransformClientRequestForRealService(_baseRequest);
            Assert.Equal(new Uri("http://real-service.com/down/the/garden"), transformed.Url);
            Assert.Equal(_baseRequest.Headers, transformed.Headers);
            Assert.Null(transformed.ContentType);
            Assert.Null(transformed.Body);
        }

        [Fact]
        public void TransformClientRequestForRealService_RequestHeaderExcludeOnlyMatchesName_ExcludesNothing()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI, new Regex[] { new Regex(@"^mock-header$") }, new Regex[0])
                .TransformClientRequestForRealService(_baseRequest);
            Assert.Equal(_baseRequest.Headers, transformed.Headers.ToArray());
        }

        [Fact]
        public void TransformClientRequestForRealService_RequestHeaderExcludeOnlyMatchesValue_ExcludesNothing()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI, new Regex[] { new Regex(@"^mock-value$") }, new Regex[0])
                .TransformClientRequestForRealService(_baseRequest);
            Assert.Equal(_baseRequest.Headers, transformed.Headers.ToArray());
        }

        [Fact]
        public void TransformClientRequestForRealService_RequestHeaderExcludeMatchesColonSeparatedNameAndValue_ExcludesMatchingHeader()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI, new Regex[] { new Regex(@"mock-header:\s*another") }, new Regex[0])
                .TransformClientRequestForRealService(_baseRequest);
            Assert.Equal(new[] { ("mock-header", "mock-value") }, transformed.Headers.ToArray());
        }

        [Fact]
        public void TransformClientRequestForRealService_RequestHeaderExcludeOnlyMatchesResponseHeader_ExcludesNothing()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI, new Regex[] { new Regex(@"mock-response-header") }, new Regex[0])
                .TransformClientRequestForRealService(_baseRequest);
            Assert.Equal(_baseRequest.Headers, transformed.Headers.ToArray());
            Assert.Equal(_baseRequest.Headers, transformed.Headers.ToArray());
        }

        [Fact]
        public void TransformClientRequestForRealService_SingleRequestHeaderExcludeMatchesMultipleValues_ExcludesAllMatchingHeaders()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI, new Regex[] { new Regex(@"mock-header") }, new Regex[0])
                .TransformClientRequestForRealService(_baseRequest);
            Assert.Empty(transformed.Headers);
        }

        [Fact]
        public void TransformClientRequestForRealService_MultipleRequestHeaderExcludeMatchDifferentValues_ExcludesAllMatchingHeaders()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI, new Regex[] { new Regex(@"^mock-header"), new Regex(@"^another-mock-header") }, new Regex[0])
                .TransformClientRequestForRealService(_baseRequest);
            Assert.Empty(transformed.Headers);
        }

        [Fact]
        public void TransformClientRequestForRealService_HostHeaderInRequestAndServiceHostSpecified_ReplacesHostHeaderValueWithRealServiceHost()
        {
            var interaction = new ServiceRequest.Builder()
                .From(_baseRequest)
                .Headers(new[] { ("Host", SERVIRTIUM_SERVICE_URI.Host) })
                .Build();
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI).TransformClientRequestForRealService(interaction);
            Assert.Equal(("Host", REAL_SERVICE_URI.Host), transformed.Headers.First());
        }

        [Fact]
        public void TransformClientRequestForRealService_HostHeaderInRequestAndNoServiceHostSpecified_LeavesHostHeaderValueTheSame()
        {
            var interaction = new ServiceRequest.Builder()
                .From(_baseRequest)
                .Headers(new[] { ("Host", SERVIRTIUM_SERVICE_URI.Host) })
                .Build();
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI).TransformClientRequestForRealService(interaction);
            Assert.Equal(("Host", REAL_SERVICE_URI.Host), transformed.Headers.First());
        }

        [Fact]
        public void TransformClientRequestForRealService_HostHeaderInRequest_RetainsCasingOfHostHeader()
        {
            var interaction = new ServiceRequest.Builder()
                .From(_baseRequest)
                .Headers(new[] { ("host", SERVIRTIUM_SERVICE_URI.Host) })
                .Build();
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI).TransformClientRequestForRealService(interaction);
            Assert.Equal(("host", REAL_SERVICE_URI.Host), transformed.Headers.First());
            interaction = new ServiceRequest.Builder()
                .From(_baseRequest)
                .Headers(new[] { ("hOsT", SERVIRTIUM_SERVICE_URI.Host) })
                .Build();
            transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI).TransformClientRequestForRealService(interaction);
            Assert.Equal(("hOsT", REAL_SERVICE_URI.Host), transformed.Headers.First());
        }

        [Fact]
        public void TransformClientRequestForRealService_HostHeaderInRequestAndHostMatchesRequestExcludePattern_RemovesHostHeader()
        {
            var interaction = new ServiceRequest.Builder()
                .From(_baseRequest)
                .Headers(new[] { ("Host", SERVIRTIUM_SERVICE_URI.Host), ("mock-header", "mock-value") })
                .Build();
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI, new[] { new Regex(@"Host") }, new Regex[0]).TransformClientRequestForRealService(interaction);
            Assert.Equal(new[] { ("mock-header", "mock-value") }, transformed.Headers);
        }

        [Fact]
        public void TransformRealServiceResponseForClient_NoRequestHeadersToRemove_ReturnsUnchangedClone()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI)
                .TransformRealServiceResponseForClient(_baseResponse);
            Assert.Equal(_baseResponse.Headers, transformed.Headers);
            Assert.Null(transformed.ContentType);
            Assert.Null(transformed.Body);
        }

        [Fact]
        public void TransformRealServiceResponseForClient_RequestHeadersToRemoveNotMatchingAndNoHostRequestHeader_ReturnsUnchangedClone()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI, new Regex[0], new Regex[] { new Regex("missing-mock-header"), new Regex("another-missing-mock-header") })
                .TransformRealServiceResponseForClient(_baseResponse);
            Assert.Equal(_baseResponse.Headers, transformed.Headers);
            Assert.Null(transformed.ContentType);
            Assert.Null(transformed.Body);
        }

        [Fact]
        public void TransformRealServiceResponseForClient_ResponseHeaderExcludeOnlyMatchesName_ExcludesNothing()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI, new Regex[0], new Regex[] { new Regex(@"^mock-response-header$") })
                .TransformRealServiceResponseForClient(_baseResponse);
            Assert.Equal(_baseResponse.Headers, transformed.Headers.ToArray());
        }

        [Fact]
        public void TransformRealServiceResponseForClient_ResponseHeaderExcludeOnlyMatchesValue_ExcludesNothing()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI, new Regex[0], new Regex[] { new Regex(@"^mock-value$") })
                .TransformRealServiceResponseForClient(_baseResponse);
            Assert.Equal(_baseResponse.Headers, transformed.Headers.ToArray());
        }

        [Fact]
        public void TransformRealServiceResponseForClient_ResponseHeaderExcludeMatchesColonSeparatedNameAndValue_ExcludesMatchingHeader()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI, new Regex[0], new Regex[] { new Regex(@"mock-response-header:\s*another") })
                .TransformRealServiceResponseForClient(_baseResponse);
            Assert.Equal(new[] { ("mock-response-header", "mock-value") }, transformed.Headers.ToArray());
        }


        [Fact]
        public void TransformRealServiceResponseForClient_SingleRequestHeaderExcludeMatchesMultipleValues_ExcludesAllMatchingHeaders()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI, new Regex[0], new Regex[] { new Regex(@"mock-response-header") })
                .TransformRealServiceResponseForClient(_baseResponse);
            Assert.Empty(transformed.Headers);
        }

        [Fact]
        public void TransformRealServiceResponseForClient_MultipleRequestHeaderExcludeMatchDifferentValues_ExcludesAllMatchingHeaders()
        {
            var transformed = new SimpleHttpMessageTransforms(REAL_SERVICE_URI, new Regex[0], new Regex[] { new Regex(@"^mock-response-header"), new Regex(@"^another-mock-response-header") })
                .TransformRealServiceResponseForClient(_baseResponse);
            Assert.Empty(transformed.Headers);
        }
    }
}
