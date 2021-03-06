﻿using Moq;
using Moq.Protected;
using Servirtium.Core.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Servirtium.Core.Tests.Http
{
    public class ServiceInteropViaSystemNetHttpTest
    {
        private Mock<HttpMessageHandler> _mockMessageHandler;
        private HttpRequestMessage? _sentRequest;

        private static string BodyAsString(byte[]? body) => Encoding.UTF8.GetString(body!);

        private HttpResponseMessage _responseToReturn = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("A RESPONSE")
        };

        public ServiceInteropViaSystemNetHttpTest()
        {
            _responseToReturn.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");

            _mockMessageHandler = new Mock<HttpMessageHandler>();
            _mockMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, cancel) =>
                {
                    _sentRequest = request;
                    return Task.FromResult(_responseToReturn);
                })
                .Verifiable();
        }

        [Fact]
        public void InvokeServiceEndpoint_ValidRequest_SendsRequestToHttpClient()
        {
            new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    new ServiceRequest.Builder()
                        .Method(HttpMethod.Get)
                        .Url(new Uri("http://a.mock.service/endpoint"))
                        .Build())
                .Wait();

            _mockMessageHandler.Protected()
                .Verify<Task<HttpResponseMessage>>("SendAsync", Times.Once(), true, ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
            Assert.Equal(HttpMethod.Get, _sentRequest!.Method);
            Assert.Equal(new Uri("http://a.mock.service/endpoint"), _sentRequest!.RequestUri);
        }

        [Fact]
        public void InvokeServiceEndpoint_RequestWithHeaders_SendsRequestWithHeaders()
        {
            new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    new ServiceRequest.Builder()
                        .Method(HttpMethod.Get)
                        .Url(new Uri("http://a.mock.service/endpoint"))
                        .Headers(new (string, string)[] {
                            ("a-request-header", "something"),
                            ("another-request-header", "something-else") })
                        .Build())
                    
                .Wait();

            _mockMessageHandler.Protected()
                .Verify<Task<HttpResponseMessage>>("SendAsync", Times.Once(), true, ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
            Assert.Equal(2, _sentRequest!.Headers.Count());
            Assert.Equal("something", _sentRequest!.Headers.GetValues("a-request-header").Single());
            Assert.Equal("something-else", _sentRequest!.Headers.GetValues("another-request-header").Single());
        }

        [Fact]
        public void InvokeServiceEndpoint_RequestWithMultiValueHeaders_SendsRequestWithHeaders()
        {
            new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    new ServiceRequest.Builder()
                        .Method(HttpMethod.Get)
                        .Url(new Uri("http://a.mock.service/endpoint"))
                        .Headers(new (string, string)[] {
                            ("a-request-header", "something"),
                            ("a-request-header", "something-else") })
                        .Build()
                    )
                .Wait();

            _mockMessageHandler.Protected()
                .Verify<Task<HttpResponseMessage>>("SendAsync", Times.Once(), true, ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
            Assert.Single(_sentRequest!.Headers);
            Assert.Equal(2, _sentRequest!.Headers.GetValues("a-request-header").Count());
            Assert.Equal("something", _sentRequest!.Headers.GetValues("a-request-header").First());
            Assert.Equal("something-else", _sentRequest!.Headers.GetValues("a-request-header").Last());
        }

        [Fact]
        public void InvokeServiceEndpoint_RequestWithBody_SendsRequestWithBody()
        {
            new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    new ServiceRequest.Builder()
                        .Method(HttpMethod.Post)
                        .Url(new Uri("http://a.mock.service/endpoint"))
                        .Body("SOME REQUEST STUFF", MediaTypeHeaderValue.Parse("text/css"))
                        .Build())
                .Wait();

            _mockMessageHandler.Protected()
                .Verify<Task<HttpResponseMessage>>("SendAsync", Times.Once(), true, ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());

            Assert.Equal(HttpMethod.Post, _sentRequest!.Method);
            Assert.Equal(new Uri("http://a.mock.service/endpoint"), _sentRequest!.RequestUri);
            Assert.Equal(MediaTypeHeaderValue.Parse("text/css"), _sentRequest!.Content.Headers.ContentType);
            Assert.Equal("SOME REQUEST STUFF", _sentRequest!.Content.ReadAsStringAsync().Result);

        }

        [Fact]
        public void InvokeServiceEndpoint_ValidRequest_ReturnsServiceResponseWithStatusCodeAndBodyOfHttpResponse()
        {
            var response = new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    new ServiceRequest.Builder()
                        .Method(HttpMethod.Get)
                        .Url(new Uri("http://a.mock.service/endpoint"))
                        .Build())
                .Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var (content, type) = response.Body!.Value;
            Assert.Equal(MediaTypeHeaderValue.Parse("application/pdf"), type);
            Assert.Equal("A RESPONSE", BodyAsString(content));
        }

        [Fact]
        public void InvokeServiceEndpoint_ResponseHasHeaders_ReturnsServiceResponseWithAdditionalContentLengthHeaderSetToBodyLength()
        {
            _responseToReturn.Headers.Add("a-response-header", "something");
            _responseToReturn.Headers.Add("another-response-header", "something-else");
            var response = new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    new ServiceRequest.Builder()
                        .Method(HttpMethod.Get)
                        .Url(new Uri("http://a.mock.service/endpoint"))
                        .Build())
                .Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var (content, type) = response.Body!.Value;
            Assert.Equal(MediaTypeHeaderValue.Parse("application/pdf"), type);
            Assert.Equal("A RESPONSE", BodyAsString(content));
            Assert.Equal(3, response.Headers.Count());
            Assert.Contains(("a-response-header", "something"), response.Headers);
            Assert.Contains(("another-response-header", "something-else"), response.Headers);
            Assert.Contains(("Content-Length", "A RESPONSE".Length.ToString()), response.Headers);
        }

        [Fact]
        public void InvokeServiceEndpoint_ResponseHasMultiValueHeaders_ReturnsServiceResponseWithHeaderItemPerValue()
        {
            _responseToReturn.Headers.Add("a-response-header", "something");
            _responseToReturn.Headers.Add("another-response-header", "something-else");
            _responseToReturn.Headers.Add("another-response-header", "also-something-else");
            var response = new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    new ServiceRequest.Builder()
                        .Method(HttpMethod.Get)
                        .Url(new Uri("http://a.mock.service/endpoint"))
                        .Build())
                .Result;
            Assert.Equal(4, response.Headers.Count());
            Assert.Contains(("a-response-header", "something"), response.Headers);
            Assert.Contains(("another-response-header", "something-else"), response.Headers);
            Assert.Contains(("another-response-header", "also-something-else"), response.Headers);
            Assert.Contains(("Content-Length", "A RESPONSE".Length.ToString()), response.Headers);
        }

        [Fact]
        public void InvokeServiceEndpoint_ResponseHasNoHeaders_ReturnsServiceResponseWithSingleContentLengthHeaderSetToBodyLength()
        {
            var response = new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    new ServiceRequest.Builder()
                        .Method(HttpMethod.Get)
                        .Url(new Uri("http://a.mock.service/endpoint"))
                        .Build())
                .Result;
            Assert.Equal("A RESPONSE", BodyAsString(response.Body!.Value.Content));
            Assert.Single(response.Headers);
            Assert.Contains(("Content-Length", "A RESPONSE".Length.ToString()), response.Headers);
        }

        [Fact]
        public void InvokeServiceEndpoint_ResponseEmptyBodyAndContentType_ReturnsServiceResponseWithNoBody()
        {
            _responseToReturn.Content = new StringContent("",Encoding.UTF8, "text/plain");
            var response = new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    new ServiceRequest.Builder()
                        .Method(HttpMethod.Get)
                        .Url(new Uri("http://a.mock.service/endpoint"))
                        .Build())
                .Result;
            Assert.False(response.Body.HasValue);
        }

        [Fact]
        public void InvokeServiceEndpoint_ResponseStringBodyAndContentType_ReturnsServiceResponseWithNoBody()
        {
            _responseToReturn.Content = new StringContent("",Encoding.UTF8, "text/plain");
            var response = new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    new ServiceRequest.Builder()
                        .Method(HttpMethod.Get)
                        .Url(new Uri("http://a.mock.service/endpoint"))
                        .Build())
                .Result;
            Assert.False(response.Body.HasValue);
        }

        [Fact]
        public void InvokeServiceEndpoint_ResponseEmptyBinaryContent_ReturnsServiceResponseWithNoBody()
        {
            _responseToReturn.Content = new ByteArrayContent(new byte[0]);
            var response = new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    new ServiceRequest.Builder()
                        .Method(HttpMethod.Get)
                        .Url(new Uri("http://a.mock.service/endpoint"))
                        .Build())
                .Result;
            Assert.False(response.Body.HasValue);
        }

        [Fact]
        public void InvokeServiceEndpoint_ResponseWithBinaryContentAndNoContentType_ReturnsServiceResponseWithBody()
        {
            _responseToReturn.Content = new ByteArrayContent(new byte[3] {1,2,3});
            var response = new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    new ServiceRequest.Builder()
                        .Method(HttpMethod.Get)
                        .Url(new Uri("http://a.mock.service/endpoint"))
                        .Build())
                .Result;
            Assert.False(response.Body.HasValue);
        }

        [Fact]
        public void InvokeServiceEndpoint_ResponseWithBinaryContentAndContentType_ReturnsServiceResponseWithBody()
        {
            _responseToReturn.Content = new ByteArrayContent(new byte[3] {1,2,3});
            _responseToReturn.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            var response = new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    new ServiceRequest.Builder()
                        .Method(HttpMethod.Get)
                        .Url(new Uri("http://a.mock.service/endpoint"))
                        .Build())
                .Result;
            var (content, type) = response.Body!.Value;
            Assert.Equal(type, MediaTypeHeaderValue.Parse("application/octet-stream"));
            Assert.Equal(3, content.Length);
        }

        [Fact]
        public void InvokeServiceEndpoint_ResponseWitTextContentAndContentType_ReturnsServiceResponseWithBody()
        {
            _responseToReturn.Content = new StringContent("something",Encoding.UTF8, "text/plain");
            var response = new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    new ServiceRequest.Builder()
                        .Method(HttpMethod.Get)
                        .Url(new Uri("http://a.mock.service/endpoint"))
                        .Build())
                .Result;
            var (content, type) = response.Body!.Value;
            Assert.Equal(type, MediaTypeHeaderValue.Parse("text/plain; charset=utf-8"));
            Assert.Equal("something", Encoding.UTF8.GetString(content));
        }
    }
}
