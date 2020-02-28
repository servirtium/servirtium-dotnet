using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace Servirtium.Core.Tests
{
    public class InteractionReplayerTest
    {
        private readonly Mock<IScriptReader> _mockScriptReader;
        private readonly Mock<IInteraction> _mockRecordedInteraction;
        private readonly Mock<IRequestMessage> _mockValidRequest;

        private IDictionary<int, IInteraction> _baselineInteractions = new Dictionary<int, IInteraction>();

        private static string BodyAsString(byte[]? body) => Encoding.UTF8.GetString(body!);
        public InteractionReplayerTest() 
        {
            _mockScriptReader = new Mock<IScriptReader>();
            _mockScriptReader.Setup(sr => sr.Read(It.IsAny<TextReader>())).Returns<TextReader>(tr => _baselineInteractions);

            _mockRecordedInteraction = new Mock<IInteraction>();
            _mockRecordedInteraction.SetupGet(i => i.Number).Returns(1337);
            _mockRecordedInteraction.SetupGet(i => i.Path).Returns("/mock/request/path");
            _mockRecordedInteraction.SetupGet(i => i.Method).Returns(HttpMethod.Get);
            _mockRecordedInteraction.SetupGet(i => i.RequestHeaders).Returns(new[] { ("header-name", "header-value"),("another-header-name", "another header-value") });
            _mockRecordedInteraction.SetupGet(i => i.StatusCode).Returns(HttpStatusCode.OK); 
            _mockRecordedInteraction.SetupGet(i => i.ResponseBody).Returns("the response body");
            _mockRecordedInteraction.SetupGet(i => i.ResponseContentType).Returns(MediaTypeHeaderValue.Parse("text/plain"));
            _mockRecordedInteraction.SetupGet(i => i.HasResponseBody).Returns(true);

            _baselineInteractions[1337] = _mockRecordedInteraction.Object;

            _mockValidRequest = new Mock<IRequestMessage>();
            _mockValidRequest.SetupGet(i => i.Url).Returns(new Uri("http://some-host.com/mock/request/path"));
            _mockValidRequest.SetupGet(i => i.Method).Returns(HttpMethod.Get);
            _mockValidRequest.SetupGet(i => i.Headers).Returns(new[] { ("header-name", "header-value"), ("another-header-name", "another header-value") });

        }

        private InteractionReplayer GenerateReplayer()
        {
            var replayer = new InteractionReplayer(_mockScriptReader.Object);
            replayer.ReadPlaybackConversation(new StringReader("some script content"));
            return replayer;
        }

        private void AddBodyToRequest(Mock<IRequestMessage> requestMock)
        {
            requestMock.Setup(i => i.HasBody).Returns(true);
            requestMock.Setup(i => i.Body).Returns(Encoding.UTF8.GetBytes("A request body"));
            requestMock.Setup(i => i.ContentType).Returns(new MediaTypeHeaderValue("text/html"));
            requestMock.Setup(i => i.Method).Returns(HttpMethod.Post);
        }


        private void AddRequestBodyToInteraction(Mock<IInteraction> interactionMock)
        {
            interactionMock.Setup(i => i.HasRequestBody).Returns(true);
            interactionMock.Setup(i => i.RequestBody).Returns("A request body");
            interactionMock.Setup(i => i.RequestContentType).Returns(new MediaTypeHeaderValue("text/html"));
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
            string responseBodyString = BodyAsString(response.Body);
            Assert.Equal(_mockRecordedInteraction.Object.ResponseBody, responseBodyString); 
            Assert.Equal(_mockRecordedInteraction.Object.StatusCode, response.StatusCode); 
            Assert.Equal(_mockRecordedInteraction.Object.ResponseContentType!.ToString(), response.ContentType!.ToString());
            Assert.Equal(_mockRecordedInteraction.Object.ResponseHeaders.ToString(), response.Headers.ToString());
        }

        [Fact]
        public void GetServiceResponseForRequest_InteractionNumberRequestMethodHeadersPathAndBodyMatchARecordedInteraction_ReturnsServiceResponseBasedOnInteractionResponse()
        {
            var replayer = GenerateReplayer();
            var response = replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result;

            AddRequestBodyToInteraction(_mockRecordedInteraction);
            AddBodyToRequest(_mockValidRequest);

            string responseBodyString = BodyAsString(response.Body);
            Assert.Equal(_mockRecordedInteraction.Object.ResponseBody, responseBodyString);
            Assert.Equal(_mockRecordedInteraction.Object.StatusCode, response.StatusCode);
            Assert.Equal(_mockRecordedInteraction.Object.ResponseContentType!.ToString(), response.ContentType!.ToString());
            Assert.Equal(_mockRecordedInteraction.Object.ResponseHeaders.ToString(), response.Headers.ToString());
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
            string responseBodyString = BodyAsString(response.Body);
            Assert.Equal(_mockRecordedInteraction.Object.ResponseBody, responseBodyString);
            Assert.Equal(_mockRecordedInteraction.Object.StatusCode, response.StatusCode);
            Assert.Equal(_mockRecordedInteraction.Object.ResponseContentType!.ToString(), response.ContentType!.ToString());
            Assert.Equal(_mockRecordedInteraction.Object.ResponseHeaders.ToString(), response.Headers.ToString());
        }

        [Fact]
        public void GetServiceResponseForRequest_HeaderSameButDifferentOrderToRecordedInteractionRequestHeaders_Returns()
        {
            var replayer = GenerateReplayer();
            _mockValidRequest.Setup(r => r.Headers).Returns(new[] { ("another-header-name", "another header-value"), ("header-name", "header-value") });
            var response = replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result;
            string responseBodyString = BodyAsString(response.Body);
            Assert.Equal(_mockRecordedInteraction.Object.ResponseBody, responseBodyString);

            Assert.Equal(_mockRecordedInteraction.Object.StatusCode, response.StatusCode);
            Assert.Equal(_mockRecordedInteraction.Object.ResponseContentType!.ToString(), response.ContentType!.ToString());
            Assert.Equal(_mockRecordedInteraction.Object.ResponseHeaders.ToString(), response.Headers.ToString());
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
            _mockValidRequest.Setup(r => r.Body).Returns(Encoding.UTF8.GetBytes("Another request body."));

            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_RequestContentTypeMismatch_Throws()
        {
            var replayer = GenerateReplayer();
            var response = replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result;

            AddRequestBodyToInteraction(_mockRecordedInteraction);
            AddBodyToRequest(_mockValidRequest);
            _mockValidRequest.Setup(r => r.ContentType).Returns(MediaTypeHeaderValue.Parse("text/html; charset=UTF-8"));

            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result);
        }
    }
}
