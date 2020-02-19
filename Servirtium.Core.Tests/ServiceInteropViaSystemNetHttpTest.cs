using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Servirtium.Core.Tests
{
    public class ServiceInteropViaSystemNetHttpTest
    {
        private Mock<HttpMessageHandler> _mockMessageHandler;
        private HttpRequestMessage? _sentRequest;

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
                    HttpMethod.Get, 
                    null, 
                    null, 
                    new Uri("http://a.mock.service/endpoint"), 
                    new (string, string)[0])
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
                    HttpMethod.Get,
                    null,
                    null,
                    new Uri("http://a.mock.service/endpoint"),
                    new (string, string)[] { 
                        ("a-request-header", "something"), 
                        ("another-request-header", "something-else") })
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
                    HttpMethod.Get,
                    null,
                    null,
                    new Uri("http://a.mock.service/endpoint"),
                    new (string, string)[] {
                        ("a-request-header", "something"),
                        ("a-request-header", "something-else") })
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
                    HttpMethod.Post,
                    "SOME REQUEST STUFF",
                    MediaTypeHeaderValue.Parse("text/css"),
                    new Uri("http://a.mock.service/endpoint"),
                    new (string, string)[0])
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
                    HttpMethod.Get,
                    null,
                    null,
                    new Uri("http://a.mock.service/endpoint"),
                    new (string, string)[0])
                .Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeHeaderValue.Parse("application/pdf"), response.ContentType);
            Assert.Equal("A RESPONSE", response.Body);
        }

        [Fact]
        public void InvokeServiceEndpoint_ResponseHasHeaders_ReturnsServiceResponseWithAdditionalContentLengthHeaderSetToBodyLength()
        {
            _responseToReturn.Headers.Add("a-response-header", "something");
            _responseToReturn.Headers.Add("another-response-header", "something-else");
            var response = new ServiceInteropViaSystemNetHttp(new HttpClient(_mockMessageHandler.Object, true))
                .InvokeServiceEndpoint(
                    HttpMethod.Get,
                    null,
                    null,
                    new Uri("http://a.mock.service/endpoint"),
                    new (string, string)[0])
                .Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeHeaderValue.Parse("application/pdf"), response.ContentType);
            Assert.Equal("A RESPONSE", response.Body);
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
                    HttpMethod.Get,
                    null,
                    null,
                    new Uri("http://a.mock.service/endpoint"),
                    new (string, string)[0])
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
                    HttpMethod.Get,
                    null,
                    null,
                    new Uri("http://a.mock.service/endpoint"),
                    new (string, string)[0])
                .Result;
            Assert.Equal("A RESPONSE", response.Body);
            Assert.Single(response.Headers);
            Assert.Contains(("Content-Length", "A RESPONSE".Length.ToString()), response.Headers);
        }
}
