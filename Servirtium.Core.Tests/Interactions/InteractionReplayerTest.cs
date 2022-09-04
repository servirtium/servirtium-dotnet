using Moq;
using Servirtium.Core.Http;
using Servirtium.Core.Interactions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace Servirtium.Core.Tests.Interactions
{
    public class InteractionReplayerTest
    {
        private readonly Mock<IScriptReader> _mockScriptReader;
        private readonly Mock<IInteraction> _mockRecordedInteraction;
        private readonly Mock<IRequestMessage> _mockValidRequest;

        private IDictionary<int, IInteraction> _baselineInteractions = new Dictionary<int, IInteraction>();

        private static string BodyAsString(byte[]? body) => Encoding.UTF8.GetString(body!);

        private static Mock<IBodyFormatter> MockCustomBodyFormatter(){
            var formatterMock = new Mock<IBodyFormatter>();
            formatterMock.Setup(f => f.Write(It.IsAny<byte[]>(), It.IsAny<MediaTypeHeaderValue>()))
                .Returns("CUSTOM FORMATTED BODY");
            formatterMock.Setup(f => f.Read(It.IsAny<string>(), It.IsAny<MediaTypeHeaderValue>()))
                .Returns(Encoding.UTF8.GetBytes("CUSTOM FORMATTED BODY"));
            return formatterMock;
        }

        private static Mock<IInteraction> MockInteraction(int interactionNumber, string path, HttpStatusCode responseCode)
        {
            var mockInteraction = new Mock<IInteraction>();
            mockInteraction.SetupGet(i => i.Number).Returns(interactionNumber);
            mockInteraction.SetupGet(i => i.Path).Returns(path);
            mockInteraction.SetupGet(i => i.Method).Returns(HttpMethod.Get);
            mockInteraction.SetupGet(i => i.RequestHeaders).Returns(new[] { ("header-name", "header-value"), ("another-header-name", "another header-value") });
            mockInteraction.SetupGet(i => i.StatusCode).Returns(responseCode);
            mockInteraction.SetupGet(i => i.ResponseBody).Returns(("the response body", MediaTypeHeaderValue.Parse("text/plain")));
            return mockInteraction;
        }

        private static Mock<IRequestMessage> MockValidRequest(string path)
        {
            var mockValidRequest = new Mock<IRequestMessage>();
            mockValidRequest.SetupGet(i => i.Url).Returns(new Uri(path));
            mockValidRequest.SetupGet(i => i.Method).Returns(HttpMethod.Get);
            mockValidRequest.SetupGet(i => i.Headers).Returns(new[] { ("header-name", "header-value"), ("another-header-name", "another header-value") });
            return mockValidRequest;
        }

        public InteractionReplayerTest() 
        {
            _mockScriptReader = new Mock<IScriptReader>();
            _mockScriptReader.Setup(sr => sr.Read(It.IsAny<TextReader>())).Returns<TextReader>(tr => _baselineInteractions);

            _mockRecordedInteraction = MockInteraction(1337, "/mock/request/path", HttpStatusCode.OK);

            _baselineInteractions[1337] = _mockRecordedInteraction.Object;

            _mockValidRequest = MockValidRequest("http://some-host.com/mock/request/path");

        }

        private InteractionReplayer GenerateReplayer(IBodyFormatter? requestBodyFormatter = null, IBodyFormatter?
            responseBodyFormatter = null)
        {
            return GenerateReplayer(TimeSpan.Zero, requestBodyFormatter, responseBodyFormatter);
        }

        private InteractionReplayer GenerateReplayer(TimeSpan outOfOrderBufferTimeout, IBodyFormatter? requestBodyFormatter = null, IBodyFormatter?
            responseBodyFormatter = null)
        {
            var replayer = new InteractionReplayer(_mockScriptReader.Object, outOfOrderBufferTimeout, null, responseBodyFormatter, requestBodyFormatter);
            replayer.ReadPlaybackConversation(new StringReader("some script content"));
            return replayer;
        }

        private void AddBodyToRequest(Mock<IRequestMessage> requestMock)
        {
            requestMock.Setup(i => i.Body).Returns((Encoding.UTF8.GetBytes("A request body"), MediaTypeHeaderValue.Parse("text/html")));
            requestMock.Setup(i => i.Method).Returns(HttpMethod.Post);
        }


        private void AddRequestBodyToInteraction(Mock<IInteraction> interactionMock)
        {
            interactionMock.Setup(i => i.RequestBody).Returns(("A request body", new MediaTypeHeaderValue("text/html")));
            interactionMock.Setup(i => i.Method).Returns(HttpMethod.Post);
        }

        [Fact]
        public void ReadPlaybackConversation_ValidTextReaderAndScriptReader_ReadsTextReaderWithScriptReader()
        {
            var textReader = new StringReader("some script content");
            new InteractionReplayer(_mockScriptReader.Object).ReadPlaybackConversation(textReader);
            _mockScriptReader.Verify(sr => sr.Read(textReader));
        }

        [Fact]
        public void GetServiceResponseForRequest_InteractionNumberRequestMethodHeadersAndPathMatchARecordedInteraction_ReturnsServiceResponseBasedOnInteractionResponse()
        {
            var replayer = GenerateReplayer();
            var response = replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result;
            var (content, type) = response.Body!.Value;
            var (recordedContent, recordedType) = _mockRecordedInteraction.Object.ResponseBody!.Value;
            string responseBodyString = BodyAsString(content);
            Assert.Equal(recordedContent, responseBodyString); 
            Assert.Equal(_mockRecordedInteraction.Object.StatusCode, response.StatusCode); 
            Assert.Equal(recordedType, type);
            Assert.Equal(_mockRecordedInteraction.Object.ResponseHeaders.ToString(), response.Headers.ToString());
        }

        [Fact]
        public void GetServiceResponseForRequest_InteractionNumberRequestMethodHeadersPathAndBodyMatchARecordedInteraction_ReturnsServiceResponseBasedOnInteractionResponse()
        {
            var replayer = GenerateReplayer();
            var response = replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result;

            AddRequestBodyToInteraction(_mockRecordedInteraction);
            AddBodyToRequest(_mockValidRequest);
            var (content, type) = response.Body!.Value;
            var (recordedContent, recordedType) = _mockRecordedInteraction.Object.ResponseBody!.Value;

            string responseBodyString = BodyAsString(content);
            Assert.Equal(recordedContent, responseBodyString);
            Assert.Equal(_mockRecordedInteraction.Object.StatusCode, response.StatusCode);
            Assert.Equal(recordedType, type);
            Assert.Equal(_mockRecordedInteraction.Object.ResponseHeaders, response.Headers);
        }

        [Fact]
        public void GetServiceResponseForRequest_ResponseHasBody_ReturnsServiceResponseWithBodyReadByBodyFormatter()
        {
            var replayer = GenerateReplayer(null, MockCustomBodyFormatter().Object);
            var response = replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result;

            Assert.Equal("CUSTOM FORMATTED BODY", Encoding.UTF8.GetString(response.Body!.Value.Content!));
        }

        [Fact]
        public void GetServiceResponseForRequest_NoRecordedInteractionWithTheSameNumber_Throws()
        {
            var replayer = GenerateReplayer();
            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(1338, _mockValidRequest.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_PathMismatchWithRecordedInteraction_Throws()
        {
            var replayer = GenerateReplayer();
            _mockValidRequest.Setup(i => i.Url).Returns(new Uri("http://some-host.com/other/request/path"));
            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result);
        }

        [Fact]
        public void Fooooo()
        {
            var replayer = new InteractionReplayer(null, null, null, null, null);
            replayer.ReadPlaybackConversation(new StringReader("some script content"), "../../DoesNotExist.md");
        }

        [Fact]
        public void GetServiceResponseForRequest_MethodMismatchWithRecordedInteraction_Throws()
        {
            var replayer = GenerateReplayer();
            _mockValidRequest.Setup(i => i.Method).Returns(HttpMethod.Delete);
            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_HeaderSupersetOfRecordedInteractionRequestHeaders_Throws()
        {
            var replayer = GenerateReplayer();
            _mockValidRequest.Setup(h => h.Headers).Returns(new[] { ("header-name", "header-value"), ("another-header-name", "another header-value"), ("yet-another-header-name", "yet-another header-value") });
            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_HeaderSubsetOfRecordedInteractionRequestHeaders_Returns()
        {
            var replayer = GenerateReplayer();
            _mockValidRequest.Setup(r => r.Headers).Returns(new[] { ("header-name", "header-value")});
            var response=replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result;

            var (content, type) = response.Body!.Value;
            var (recordedContent, recordedType) = _mockRecordedInteraction.Object.ResponseBody!.Value;

            string responseBodyString = BodyAsString(content);
            Assert.Equal(recordedContent, responseBodyString);
            Assert.Equal(_mockRecordedInteraction.Object.StatusCode, response.StatusCode);
            Assert.Equal(recordedType, type);
            Assert.Equal(_mockRecordedInteraction.Object.ResponseHeaders, response.Headers);
        }

        [Fact]
        public void GetServiceResponseForRequest_HeaderSameButDifferentOrderToRecordedInteractionRequestHeaders_Returns()
        {
            var replayer = GenerateReplayer();
            _mockValidRequest.Setup(r => r.Headers).Returns(new[] { ("another-header-name", "another header-value"), ("header-name", "header-value") });
            var response = replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result;

            var (content, type) = response.Body!.Value;
            var (recordedContent, recordedType) = _mockRecordedInteraction.Object.ResponseBody!.Value;
            string responseBodyString = BodyAsString(content);

            Assert.Equal(recordedContent, responseBodyString);
            Assert.Equal(_mockRecordedInteraction.Object.StatusCode, response.StatusCode);
            Assert.Equal(recordedType, type);
            Assert.Equal(_mockRecordedInteraction.Object.ResponseHeaders, response.Headers);
        }

        [Fact]
        public void GetServiceResponseForRequest_RequestHasBodyButRecordedInteractionDoesnt_Throws()
        {
            var replayer = GenerateReplayer();
            var response = replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result;

            AddBodyToRequest(_mockValidRequest);

            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_RequestHasNoBodyButRecordedInteractionDoes_Throws()
        {
            var replayer = GenerateReplayer();
            var response = replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result;

            AddRequestBodyToInteraction(_mockRecordedInteraction);

            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_RequestBodyMismatch_Throws()
        {
            var replayer = GenerateReplayer();
            var response = replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result;

            AddRequestBodyToInteraction(_mockRecordedInteraction);
            AddBodyToRequest(_mockValidRequest);
            _mockValidRequest.Setup(r => r.Body).Returns((Encoding.UTF8.GetBytes("Another request body."), MediaTypeHeaderValue.Parse("text/plain")));

            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_RequestBodyFormattedByFormatterMatchesRecordedInteractionRequestBody_DoesNotThrow()
        {
            var replayer = GenerateReplayer(MockCustomBodyFormatter().Object);

            AddRequestBodyToInteraction(_mockRecordedInteraction);
            _mockRecordedInteraction.Setup(i => i.RequestBody).Returns(("CUSTOM FORMATTED BODY",MediaTypeHeaderValue.Parse("text/html")));
            AddBodyToRequest(_mockValidRequest);
            replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Wait();
        }

        [Fact]
        public void GetServiceResponseForRequest_RequestContentTypeMismatch_Throws()
        {
            var replayer = GenerateReplayer();
            var response = replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result;

            AddRequestBodyToInteraction(_mockRecordedInteraction);
            AddBodyToRequest(_mockValidRequest);
            _mockValidRequest.Setup(r => r.Body).Returns((Encoding.UTF8.GetBytes("the request body"), MediaTypeHeaderValue.Parse("text/html; charset=UTF-8")));

            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_InvalidButValidForInteractionMatchedAgainstOtherConcurrentRequest_UsesOtherInteraction()
        {
            var replayer = GenerateReplayer(TimeSpan.FromSeconds(1));          
            var mockOtherInteraction = MockInteraction(1338, "/mock/request/path2", HttpStatusCode.NotFound);
            _baselineInteractions[1338] = mockOtherInteraction.Object;
            var mockOtherRequest = MockValidRequest("http://some-host.com/mock/request/path2");
            var otherRequestTask = replayer.GetServiceResponseForRequest(1337, mockOtherRequest.Object);
            var requestTask = replayer.GetServiceResponseForRequest(1338, _mockValidRequest.Object);
            var otherResponse = otherRequestTask.Result;
            var response = requestTask.Result;
            Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void GetServiceResponseForRequest_MultipleConcurrentRequestsOutOfOrder_MatchesAgainstCorrectRequests()
        {
            var replayer = GenerateReplayer(TimeSpan.FromSeconds(1));
            var mockInteraction2 = MockInteraction(1338, "/mock/request/path2", HttpStatusCode.NotFound);
            _baselineInteractions[1338] = mockInteraction2.Object;
            var mockInteraction3 = MockInteraction(1339, "/mock/request/path3", HttpStatusCode.Created);
            _baselineInteractions[1339] = mockInteraction3.Object;
            var mockInteraction4 = MockInteraction(1340, "/mock/request/path4", HttpStatusCode.Forbidden);
            _baselineInteractions[1340] = mockInteraction4.Object;

            var mockRequest2 = MockValidRequest("http://some-host.com/mock/request/path2");
            var mockRequest3 = MockValidRequest("http://some-host.com/mock/request/path3");
            var mockRequest4 = MockValidRequest("http://some-host.com/mock/request/path4");

            var request2Task = replayer.GetServiceResponseForRequest(1337, mockRequest2.Object);
            var request4Task = replayer.GetServiceResponseForRequest(1338, mockRequest4.Object);
            var request3Task = replayer.GetServiceResponseForRequest(1339, mockRequest3.Object);
            var requestTask = replayer.GetServiceResponseForRequest(1340, _mockValidRequest.Object);

            var response = requestTask.Result;
            var request2Response = request2Task.Result;
            var request3Response = request3Task.Result;
            var request4Response = request4Task.Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, request2Response.StatusCode);
            Assert.Equal(HttpStatusCode.Created, request3Response.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, request4Response.StatusCode);
        }
    }
}
