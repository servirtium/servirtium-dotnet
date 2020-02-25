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
        private readonly Mock<IInteraction> _mockValidRequestInteraction;

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

            _mockValidRequestInteraction = new Mock<IInteraction>();
            _mockValidRequestInteraction.SetupGet(i => i.Number).Returns(1337);
            _mockValidRequestInteraction.SetupGet(i => i.Path).Returns("/mock/request/path");
            _mockValidRequestInteraction.SetupGet(i => i.Method).Returns(HttpMethod.Get);
            _mockValidRequestInteraction.SetupGet(i => i.RequestHeaders).Returns(new[] { ("header-name", "header-value"), ("another-header-name", "another header-value") });

        }

        private InteractionReplayer GenerateReplayer()
        {
            var replayer = new InteractionReplayer(_mockScriptReader.Object);
            replayer.ReadPlaybackConversation(new StringReader("some script content"));
            return replayer;
        }

        private void AddBodyToRequest(Mock<IInteraction> interactionMock)
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
            var response = replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result;
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
            var response = replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result;

            AddBodyToRequest(_mockRecordedInteraction);
            AddBodyToRequest(_mockValidRequestInteraction);

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
            _mockValidRequestInteraction.Setup(i => i.Number).Returns(1338);
            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_PathMismatchWithRecordedInteraction_Throws()
        {
            var replayer = GenerateReplayer();
            _mockValidRequestInteraction.Setup(i => i.Path).Returns("/other/request/path");
            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_MethodMismatchWithRecordedInteraction_Throws()
        {
            var replayer = GenerateReplayer();
            _mockValidRequestInteraction.Setup(i => i.Method).Returns(HttpMethod.Delete);
            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_HeaderSupersetOfRecordedInteractionRequestHeaders_Throws()
        {
            var replayer = GenerateReplayer();
            _mockValidRequestInteraction.Setup(i => i.RequestHeaders).Returns(new[] { ("header-name", "header-value"), ("another-header-name", "another header-value"), ("yet-another-header-name", "yet-another header-value") });
            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_HeaderSubsetOfRecordedInteractionRequestHeaders_Returns()
        {
            var replayer = GenerateReplayer();
            _mockValidRequestInteraction.Setup(i => i.RequestHeaders).Returns(new[] { ("header-name", "header-value")});
            var response=replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result;
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
            _mockValidRequestInteraction.Setup(i => i.RequestHeaders).Returns(new[] { ("another-header-name", "another header-value"), ("header-name", "header-value") });
            var response = replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result;
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
            var response = replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result;

            AddBodyToRequest(_mockRecordedInteraction);

            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_RequestHasNoBodyButRecordedInteractionDoes_Throws()
        {
            var replayer = GenerateReplayer();
            var response = replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result;

            AddBodyToRequest(_mockValidRequestInteraction);

            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_RequestBodyMismatch_Throws()
        {
            var replayer = GenerateReplayer();
            var response = replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result;

            AddBodyToRequest(_mockRecordedInteraction);
            AddBodyToRequest(_mockValidRequestInteraction);
            _mockValidRequestInteraction.Setup(i => i.RequestBody).Returns("Another request body.");

            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result);
        }

        [Fact]
        public void GetServiceResponseForRequest_RequestContentTypeMismatch_Throws()
        {
            var replayer = GenerateReplayer();
            var response = replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result;

            AddBodyToRequest(_mockRecordedInteraction);
            AddBodyToRequest(_mockValidRequestInteraction);
            _mockValidRequestInteraction.Setup(i => i.RequestContentType).Returns(MediaTypeHeaderValue.Parse("text/html; charset=UTF-8"));

            Assert.ThrowsAny<Exception>(() => replayer.GetServiceResponseForRequest(new Uri("http://some-host.com/mock/request/path"), _mockValidRequestInteraction.Object).Result);
        }
    }
}
