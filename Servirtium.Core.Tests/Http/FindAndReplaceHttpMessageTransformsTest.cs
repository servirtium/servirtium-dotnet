using Servirtium.Core.Http;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Servirtium.Core.Tests.Http
{
    public class FindAndReplaceHttpMessageTransformsTest
    {
        private static readonly ServiceRequest REQUEST_WITH_TEXT_BODY = new ServiceRequest.Builder()
            .Body("I want to eat, eat ,eat apples and bananas", MediaTypeHeaderValue.Parse("text/plain"))
            .Headers(("want","bananas"), ("eat", "apples"))
            .Build();
        private static readonly ServiceRequest REQUEST_WITHOUT_BODY = new ServiceRequest.Builder()
            .Headers(("want", "bananas"), ("eat", "apples"))
            .Build();
        private static readonly ServiceRequest REQUEST_WITH_BINARY_BODY = new ServiceRequest.Builder()
            .Body("I want to eat, eat ,eat apples and bananas", MediaTypeHeaderValue.Parse("application/pdf"))
            .Headers(("want", "bananas"), ("eat", "apples"))
            .Build();
        private static readonly ServiceResponse RESPONSE_WITH_TEXT_BODY = new ServiceResponse.Builder()
            .Body("I want to eat, eat ,eat apples and bananas", MediaTypeHeaderValue.Parse("text/plain"))
            .Headers(("want", "bananas"), ("eat", "apples"))
            .Build();
        private static readonly ServiceResponse RESPONSE_WITHOUT_BODY = new ServiceResponse.Builder()
            .Headers(("want", "bananas"), ("eat", "apples"))
            .Build();
        private static readonly ServiceResponse RESPONSE_WITH_BINARY_BODY = new ServiceResponse.Builder()
            .Body("I want to eat, eat ,eat apples and bananas", MediaTypeHeaderValue.Parse("application/pdf"))
            .Headers(("want", "bananas"), ("eat", "apples"))
            .Build();

        [Fact]
        public void TransformClientRequestForRealService_NoReplacements_ReturnsSameRequest()
        {
            var transformed = new FindAndReplaceHttpMessageTransforms(new RegexReplacement[0]).TransformClientRequestForRealService(REQUEST_WITH_TEXT_BODY);
            Assert.Equal(REQUEST_WITH_TEXT_BODY, transformed);
        }

        [Fact]
        public void TransformClientRequestForRealService_OnlyResponseReplacements_ReturnsSameRequest()
        {
            var transformed = new FindAndReplaceHttpMessageTransforms(
                new RegexReplacement[] { 
                    new RegexReplacement(new Regex("bananas"), "bununus", ReplacementContext.ResponseBody),
                    new RegexReplacement(new Regex("apples"), "upples", ReplacementContext.ResponseHeader)
                }).TransformClientRequestForRealService(REQUEST_WITH_TEXT_BODY);
            Assert.Equal(REQUEST_WITH_TEXT_BODY, transformed);
        }

        [Fact]
        public void TransformClientRequestForRealService_ScopedRequestReplacements_OnlyReplacesInScope()
        {
            var transformed = new FindAndReplaceHttpMessageTransforms(
                new RegexReplacement[] {
                    new RegexReplacement(new Regex("bananas"), "bununus", ReplacementContext.RequestBody),
                    new RegexReplacement(new Regex("apples"), "upples", ReplacementContext.RequestHeader)
                }).TransformClientRequestForRealService(REQUEST_WITH_TEXT_BODY);
            Assert.Equal(
                new ServiceRequest.Builder()
                    .From(REQUEST_WITH_TEXT_BODY)
                    .Headers(("want", "bananas"), ("eat", "upples"))
                    .Body("I want to eat, eat ,eat apples and bununus", MediaTypeHeaderValue.Parse("text/plain"))
                    .Build(),
                transformed
            );
        }

        [Fact]
        public void TransformClientRequestForRealService_MultiScopedRequestReplacements_ReplacesInHeaderAndBody()
        {
            var transformed = new FindAndReplaceHttpMessageTransforms(
                new RegexReplacement[] {
                    new RegexReplacement(new Regex("bananas"), "bununus", ReplacementContext.RequestBody | ReplacementContext.RequestHeader),
                    new RegexReplacement(new Regex("apples"), "upples", ReplacementContext.RequestHeader)
                }).TransformClientRequestForRealService(REQUEST_WITH_TEXT_BODY);
            Assert.Equal(
                new ServiceRequest.Builder()
                    .From(REQUEST_WITH_TEXT_BODY)
                    .Headers(("want", "bununus"), ("eat", "upples"))
                    .Body("I want to eat, eat ,eat apples and bununus", MediaTypeHeaderValue.Parse("text/plain"))
                    .Build(),
                transformed
            );
        }

        [Fact]
        public void TransformClientRequestForRealService_MultipleRequestReplacements_ReplacesInOrderSpecified()
        {
            var transformed = new FindAndReplaceHttpMessageTransforms(
                new RegexReplacement[] {
                    new RegexReplacement(new Regex("b[a-z]n[a-z]n[a-z]s"), "bununus", ReplacementContext.RequestHeader),
                    new RegexReplacement(new Regex("b[a-z]n[a-z]n[a-z]s"), "bononos", ReplacementContext.RequestHeader)
                }).TransformClientRequestForRealService(REQUEST_WITH_TEXT_BODY);
            Assert.Equal(
                new ServiceRequest.Builder()
                    .From(REQUEST_WITH_TEXT_BODY)
                    .Headers(("want", "bononos"), ("eat", "apples"))
                    .Build(),
                transformed
            );
        }

        [Fact]
        public void TransformClientRequestForRealService_RequestHasNoBody_IgnoresRequestBodyReplacements()
        {
            var transformed = new FindAndReplaceHttpMessageTransforms(
                new RegexReplacement[] {
                    new RegexReplacement(new Regex("bananas"), "bununus", ReplacementContext.RequestBody | ReplacementContext.RequestHeader),
                    new RegexReplacement(new Regex("apples"), "upples", ReplacementContext.RequestHeader)
                }).TransformClientRequestForRealService(REQUEST_WITHOUT_BODY);
            Assert.Equal(
                new ServiceRequest.Builder()
                    .From(REQUEST_WITHOUT_BODY)
                    .Headers(("want", "bununus"), ("eat", "upples"))
                    .Build(),
                transformed
            );
        }

        [Fact]
        public void TransformClientRequestForRealService_RequestHasBinaryBody_IgnoresRequestBodyReplacements()
        {
            var transformed = new FindAndReplaceHttpMessageTransforms(
                new RegexReplacement[] {
                    new RegexReplacement(new Regex("bananas"), "bununus", ReplacementContext.RequestBody | ReplacementContext.RequestHeader),
                    new RegexReplacement(new Regex("apples"), "upples", ReplacementContext.RequestHeader)
                }).TransformClientRequestForRealService(REQUEST_WITH_BINARY_BODY);
            Assert.Equal(
                new ServiceRequest.Builder()
                    .From(REQUEST_WITH_BINARY_BODY)
                    .Headers(("want", "bununus"), ("eat", "upples"))
                    .Build(),
                transformed
            );
        }


        [Fact]
        public void TransformRealServiceResponseForClient_NoReplacements_ReturnsSameResponse()
        {
            var transformed = new FindAndReplaceHttpMessageTransforms(new RegexReplacement[0]).TransformRealServiceResponseForClient(RESPONSE_WITH_TEXT_BODY);
            Assert.Equal(RESPONSE_WITH_TEXT_BODY, transformed);
        }

        [Fact]
        public void TransformRealServiceResponseForClient_OnlyRequestReplacements_ReturnsSameResponse()
        {
            var transformed = new FindAndReplaceHttpMessageTransforms(
                new RegexReplacement[] {
                    new RegexReplacement(new Regex("bananas"), "bununus", ReplacementContext.RequestBody),
                    new RegexReplacement(new Regex("apples"), "upples", ReplacementContext.RequestHeader)
                }).TransformRealServiceResponseForClient(RESPONSE_WITH_TEXT_BODY);
            Assert.Equal(RESPONSE_WITH_TEXT_BODY, transformed);
        }

        [Fact]
        public void TransformRealServiceResponseForClient_ScopedResponseReplacements_OnlyReplacesInScope()
        {
            var transformed = new FindAndReplaceHttpMessageTransforms(
                new RegexReplacement[] {
                    new RegexReplacement(new Regex("bananas"), "bununus", ReplacementContext.ResponseBody),
                    new RegexReplacement(new Regex("apples"), "upples", ReplacementContext.ResponseHeader)
                }).TransformRealServiceResponseForClient(RESPONSE_WITH_TEXT_BODY);
            Assert.Equal(
                new ServiceResponse.Builder()
                    .From(RESPONSE_WITH_TEXT_BODY)
                    .Headers(("want", "bananas"), ("eat", "upples"))
                    .Body("I want to eat, eat ,eat apples and bununus", MediaTypeHeaderValue.Parse("text/plain"))
                    .Build(),
                transformed
            );
        }

        [Fact]
        public void TransformRealServiceResponseForClient_MultiScopedRequestReplacements_ReplacesInHeaderAndBody()
        {
            var transformed = new FindAndReplaceHttpMessageTransforms(
                new RegexReplacement[] {
                    new RegexReplacement(new Regex("bananas"), "bununus", ReplacementContext.ResponseBody | ReplacementContext.ResponseHeader),
                    new RegexReplacement(new Regex("apples"), "upples", ReplacementContext.ResponseHeader)
                }).TransformRealServiceResponseForClient(RESPONSE_WITH_TEXT_BODY);
            Assert.Equal(
                new ServiceResponse.Builder()
                    .From(RESPONSE_WITH_TEXT_BODY)
                    .Headers(("want", "bununus"), ("eat", "upples"))
                    .Body("I want to eat, eat ,eat apples and bununus", MediaTypeHeaderValue.Parse("text/plain"))
                    .Build(),
                transformed
            );
        }

        [Fact]
        public void TransformRealServiceResponseForClient_MultipleResponseReplacements_ReplacesInOrderSpecified()
        {
            var transformed = new FindAndReplaceHttpMessageTransforms(
                new RegexReplacement[] {
                    new RegexReplacement(new Regex("b[a-z]n[a-z]n[a-z]s"), "bununus", ReplacementContext.ResponseHeader),
                    new RegexReplacement(new Regex("b[a-z]n[a-z]n[a-z]s"), "bononos", ReplacementContext.ResponseHeader)
                }).TransformRealServiceResponseForClient(RESPONSE_WITH_TEXT_BODY);
            Assert.Equal(
                new ServiceResponse.Builder()
                    .From(RESPONSE_WITH_TEXT_BODY)
                    .Headers(("want", "bononos"), ("eat", "apples"))
                    .Build(),
                transformed
            );
        }

        [Fact]
        public void TransformRealServiceResponseForClient_ResponseHasNoBody_IgnoresResponseBodyReplacements()
        {
            var transformed = new FindAndReplaceHttpMessageTransforms(
                new RegexReplacement[] {
                    new RegexReplacement(new Regex("bananas"), "bununus", ReplacementContext.ResponseBody | ReplacementContext.ResponseHeader),
                    new RegexReplacement(new Regex("apples"), "upples", ReplacementContext.ResponseHeader)
                }).TransformRealServiceResponseForClient(RESPONSE_WITHOUT_BODY);
            Assert.Equal(
                new ServiceResponse.Builder()
                    .From(RESPONSE_WITHOUT_BODY)
                    .Headers(("want", "bununus"), ("eat", "upples"))
                    .Build(),
                transformed
            );
        }

        [Fact]
        public void TransformRealServiceResponseForClient_ResponseHasBinaryBody_IgnoresRequestBodyReplacements()
        {
            var transformed = new FindAndReplaceHttpMessageTransforms(
                new RegexReplacement[] {
                    new RegexReplacement(new Regex("bananas"), "bununus", ReplacementContext.ResponseBody | ReplacementContext.ResponseHeader),
                    new RegexReplacement(new Regex("apples"), "upples", ReplacementContext.ResponseHeader)
                }).TransformRealServiceResponseForClient(RESPONSE_WITH_BINARY_BODY);
            Assert.Equal(
                new ServiceResponse.Builder()
                    .From(RESPONSE_WITH_BINARY_BODY)
                    .Headers(("want", "bununus"), ("eat", "upples"))
                    .Build(),
                transformed
            );
        }
    }
}
