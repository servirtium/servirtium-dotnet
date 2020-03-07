using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using Servirtium.Core;
using Servirtium.Core.Http;
using Servirtium.Core.Interactions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Servirtium.AspNetCore.Tests
{
    public class AspNetCoreServirtiumRequestHandlerTest
    {

        class MockHeaders : Dictionary<string, StringValues>, IHeaderDictionary
        {
            public MockHeaders() : base(StringComparer.InvariantCultureIgnoreCase) { }
            public long? ContentLength { get; set; }
        }

        private readonly Mock<IServirtiumRequestHandler> _mockInternalHandler;

        private Mock<IHeaderDictionary> _mockRequestHeaders;

        private MockHeaders _sentResponseHeaders = new MockHeaders();

        private readonly Mock<IEnumerator<KeyValuePair<string, StringValues>>> _mockRequestHeaderEnumerator;

        private readonly MemoryStream _responseBody = new MemoryStream();

        private readonly MemoryStream _requestBody = new MemoryStream(Encoding.UTF8.GetBytes("A REQUEST BODY"));

        private readonly Mock<Action<HttpStatusCode>> _mockStatusCodeSetter;

        private readonly Mock<Action<string>> _mockResponseContentTypeSetter;

        private IRequestMessage? _requestToProcess;

        private readonly ICollection<IInteraction.Note> _notes = new IInteraction.Note[0];

        private IResponseMessage _response = new ServiceResponse.Builder()
            .Body("Rock your body right.", MediaTypeHeaderValue.Parse("text/plain"))
            .StatusCode(HttpStatusCode.OK)
            .Build();

        public AspNetCoreServirtiumRequestHandlerTest()
        {
            _mockInternalHandler = new Mock<IServirtiumRequestHandler>();
            _mockInternalHandler.Setup(h => h.ProcessRequest(It.IsAny<IRequestMessage>(), It.IsAny<IEnumerable<IInteraction.Note>>()))
                .Returns<IRequestMessage, IEnumerable<IInteraction.Note>>((req, notes)=>
                {
                    _requestToProcess = req;
                    return Task.FromResult(_response);
                });

            _mockRequestHeaderEnumerator = new Mock<IEnumerator<KeyValuePair<string, StringValues>>>();
            _mockRequestHeaders = new Mock<IHeaderDictionary>();
            _mockRequestHeaders.Setup(rh => rh.GetEnumerator()).Returns(_mockRequestHeaderEnumerator.Object);


            _mockStatusCodeSetter = new Mock<Action<HttpStatusCode>>();
            _mockResponseContentTypeSetter = new Mock<Action<string>>();
        }

        private AspNetCoreServirtiumRequestHandler RequestHandler() =>
            new AspNetCoreServirtiumRequestHandler(_mockInternalHandler.Object);

        private void HandleNoBodyRequest(AspNetCoreServirtiumRequestHandler handler, IEnumerable<IInteraction.Note>? notes = null)=>
            handler.HandleRequest(new Uri("http://a.mock.service"), "endpoint", HttpMethod.Get.Method, _mockRequestHeaders.Object, null, null, _mockStatusCodeSetter.Object, _sentResponseHeaders, _responseBody, _mockResponseContentTypeSetter.Object, notes ?? _notes).Wait();

        private void HandleBodyRequest(AspNetCoreServirtiumRequestHandler handler)
        {
            handler.HandleRequest(new Uri("http://a.mock.service"), "endpoint", HttpMethod.Post.Method, _mockRequestHeaders.Object, "text/plain", _requestBody, _mockStatusCodeSetter.Object, _sentResponseHeaders, _responseBody, _mockResponseContentTypeSetter.Object, _notes).Wait();
        }
        private void HandleRequestWithHeaders(AspNetCoreServirtiumRequestHandler handler, (string Name, string[] Values)[] headers)
        {
            _mockRequestHeaders.Setup(h => h.GetEnumerator()).Returns(
                ()=>
                    headers.Select(h => new KeyValuePair<string, StringValues>(h.Name, new StringValues(h.Values))).GetEnumerator()
                );
            handler.HandleRequest(new Uri("http://a.mock.service"), "endpoint", HttpMethod.Get.Method, _mockRequestHeaders.Object, null, null, _mockStatusCodeSetter.Object, _sentResponseHeaders, _responseBody, _mockResponseContentTypeSetter.Object, _notes).Wait();
        }


        [Fact]
        public void HandleRequest_RequestWithNoBodyOrHeaders_CallsServirtiumHandlerWithRequestBuiltFromInputs()
        {
            HandleNoBodyRequest(RequestHandler());
            _mockInternalHandler.Verify(h => h.ProcessRequest(It.IsAny<IRequestMessage>(), _notes));
            Assert.Equal(HttpMethod.Get, _requestToProcess!.Method);
            Assert.Equal(new Uri("http://a.mock.service/endpoint"), _requestToProcess.Url);
            Assert.Empty(_requestToProcess.Headers);
            Assert.False(_requestToProcess.Body.HasValue);
        }

        [Fact]
        public void HandleRequest_RequestWithBody_CallsServirtiumHandlerWithRequestBuiltFromInputs()
        {
            HandleBodyRequest(RequestHandler());
            _mockInternalHandler.Verify(h => h.ProcessRequest(It.IsAny<IRequestMessage>(), _notes));
            Assert.Equal(HttpMethod.Post, _requestToProcess!.Method);
            Assert.Equal(new Uri("http://a.mock.service/endpoint"), _requestToProcess.Url);
            Assert.Empty(_requestToProcess.Headers);
            Assert.True(_requestToProcess.Body.HasValue);
            Assert.Equal("A REQUEST BODY", Encoding.UTF8.GetString(_requestToProcess.Body!.Value.Content!));
        }

        [Fact]
        public void HandleRequest_RequestWithHeaders_CallsServirtiumHandlerWithRequestBuiltFromInputs()
        {
            HandleRequestWithHeaders(RequestHandler(), new[] {
                ("single-value", new []{ "value" }),
                ("multi-value", new []{ "value1", "value2" }),
                ("another-value", new []{ "value" })
            });
            _mockInternalHandler.Verify(h => h.ProcessRequest(It.IsAny<IRequestMessage>(), _notes));
            Assert.Equal(4, _requestToProcess!.Headers.Count());
            Assert.Contains(("single-value", "value"), _requestToProcess.Headers);
            Assert.Contains(("multi-value", "value1"), _requestToProcess.Headers);
            Assert.Contains(("multi-value", "value2"), _requestToProcess.Headers);
            Assert.Contains(("another-value", "value"), _requestToProcess.Headers);
        }


        [Fact]
        public void HandleRequest_RequestWithNotes_CallsServirtiumHandlerWithSpecifiedNotes()
        {
            var notes = new [] {
                new IInteraction.Note(IInteraction.Note.NoteType.Text, "A note", "Note content"),
                new IInteraction.Note(IInteraction.Note.NoteType.Code, "A code note", "Code note content")
                };
            HandleNoBodyRequest(RequestHandler(), notes);
            _mockInternalHandler.Verify(h => h.ProcessRequest(
                It.IsAny<IRequestMessage>(), 
                It.Is<IEnumerable<IInteraction.Note>>(n=> 
                    n.Count()==2 && n.First().Title == "A note" && n.Last().Title == "A code note"))
            );
        }

        [Fact]
        public void HandleRequest_ResponseReturnedHasNoBodyOrHeaders_CallsStatusSetterWithStatus()
        {
            _response = new ServiceResponse.Builder()
               .StatusCode(HttpStatusCode.FailedDependency)
               .Build();

            HandleNoBodyRequest(RequestHandler());
            _mockStatusCodeSetter.Verify(scs => scs(HttpStatusCode.FailedDependency));
            Assert.Equal(0, _responseBody.Length);
            _mockResponseContentTypeSetter.Verify(rcts => rcts(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public void HandleRequest_ResponseReturnedHasABodyAndNoHeaders_ResonseBodyStreamPopulatedAndContentTypeSet()
        {
            HandleNoBodyRequest(RequestHandler());
            _mockStatusCodeSetter.Verify(scs => scs(HttpStatusCode.OK));
            Assert.Equal("Rock your body right.", Encoding.UTF8.GetString(_responseBody.ToArray()));
            _mockResponseContentTypeSetter.Verify(rcts => rcts("text/plain"));
        }

        [Fact]
        public void HandleRequest_ResponseReturnedHasHeaders_HeadersCollapsedAndSet()
        {
            _response = new ServiceResponse.Builder()
                .Headers(new[] { 
                    ("response-header-1", "value1"),
                    ("response-header-2", "value2"),
                    ("response-header-2", "value3"),
                    ("response-header-2", "value4"),
                    ("response-header-3", "value5")
                })
                .Build();
            HandleNoBodyRequest(RequestHandler());
            _mockStatusCodeSetter.Verify(scs => scs(HttpStatusCode.OK));
            Assert.Equal(3, _sentResponseHeaders.Count());
            Assert.Equal("value1", _sentResponseHeaders["response-header-1"].Single());
            Assert.Equal(3, _sentResponseHeaders["response-header-2"].Count());
            Assert.Contains<string>("value2", _sentResponseHeaders["response-header-2"]);
            Assert.Contains<string>("value3", _sentResponseHeaders["response-header-2"]);
            Assert.Contains<string>("value4", _sentResponseHeaders["response-header-2"]);
            Assert.Equal("value5", _sentResponseHeaders["response-header-3"].Single());
        }

        [Fact]
        public void HandleRequest_ResponseReturnedHasSameHeadersDifferentCasings_HeadersCollapsedcaseInsensitively()
        {
            _response = new ServiceResponse.Builder()
                .Headers(new[] {
                    ("response-header-1", "value1"),
                    ("response-header-2", "value2"),
                    ("Response-Header-2", "value3"),
                    ("response-header-2", "value4"),
                    ("response-header-3", "value5")
                })
                .Build();
            HandleNoBodyRequest(RequestHandler());
            _mockStatusCodeSetter.Verify(scs => scs(HttpStatusCode.OK));
            Assert.Equal(3, _sentResponseHeaders.Count());
            Assert.Equal("value1", _sentResponseHeaders["response-header-1"].Single());
            Assert.Equal(3, _sentResponseHeaders["response-header-2"].Count());
            Assert.Contains<string>("value2", _sentResponseHeaders["response-header-2"]);
            Assert.Contains<string>("value3", _sentResponseHeaders["response-header-2"]);
            Assert.Contains<string>("value4", _sentResponseHeaders["response-header-2"]);
            Assert.Equal("value5", _sentResponseHeaders["response-header-3"].Single());
        }

        [Fact]
        public void HandleRequest_ResponseReturnedHasChunkedTransferEncodingHeader_TransferEncodingHeaderRemoved()
        {
            _response = new ServiceResponse.Builder()
                .Headers(new[] {
                    ("response-header-1", "value1"),
                    ("transfer-encoding", "chunked")

                })
                .Build();
            HandleNoBodyRequest(RequestHandler());
            Assert.Single(_sentResponseHeaders);
            Assert.DoesNotContain("transfer-encoding", _sentResponseHeaders.Keys);
        }

        [Fact]
        public void HandleRequest_ResponseReturnedHasCapitalisedChunkedTransferEncodingHeader_TransferEncodingHeaderRemoved()
        {
            _response = new ServiceResponse.Builder()
                .Headers(new[] {
                    ("response-header-1", "value1"),
                    ("Transfer-Encoding", "Chunked")

                })
                .Build();
            HandleNoBodyRequest(RequestHandler());
            Assert.Single(_sentResponseHeaders);
            Assert.DoesNotContain("transfer-encoding", _sentResponseHeaders.Keys);
            Assert.DoesNotContain("Transfer-Encoding", _sentResponseHeaders.Keys);
        }

        [Fact]
        public void HandleRequest_ResponseReturnedHasOtherTransferEncodingHeader_TransferEncodingHeaderRetained()
        {
            _response = new ServiceResponse.Builder()
                .Headers(new[] {
                    ("response-header-1", "value1"),
                    ("Transfer-Encoding", "Compress")

                })
                .Build();
            HandleNoBodyRequest(RequestHandler());
            Assert.Equal(2, _sentResponseHeaders.Count());
            Assert.Contains("Transfer-Encoding", _sentResponseHeaders.Keys);
        }

        [Fact]
        public void HandleRequest_ResponseReturnedHasMultipleTransferEncodingHeaders_OnlyChunkedValueRemoved()
        {
            _response = new ServiceResponse.Builder()
                .Headers(new[] {
                    ("Transfer-Encoding", "Chunked"),
                    ("Transfer-Encoding", "Compress")

                })
                .Build();
            HandleNoBodyRequest(RequestHandler());
            Assert.Single(_sentResponseHeaders);
            Assert.Equal("Compress", _sentResponseHeaders["Transfer-Encoding"]);
        }
    }
}
