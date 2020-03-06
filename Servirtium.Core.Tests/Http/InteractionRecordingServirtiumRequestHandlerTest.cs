using Moq;
using Servirtium.Core.Http;
using Servirtium.Core.Interactions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Servirtium.Core.Tests.Http
{
    public class InteractionRecordingServirtiumRequestHandlerTest
    {
        private readonly Mock<IInteractionMonitor> _mockMonitor = new Mock<IInteractionMonitor>();
        private readonly Mock<IHttpMessageTransforms> _mockTransforms = new Mock<IHttpMessageTransforms>();

        private readonly IResponseMessage _response = new ServiceResponse.Builder()
            .Body("Hello!", MediaTypeHeaderValue.Parse("text/csv"))
            .Headers(new[] {("a", "header") })
            .StatusCode(System.Net.HttpStatusCode.BadGateway)
            .Build();

        private readonly IResponseMessage _transformedResponse = new ServiceResponse.Builder()
            .Body("Hello!", MediaTypeHeaderValue.Parse("text/csv"))
            .Headers(new[] { ("another", "header") })
            .StatusCode(System.Net.HttpStatusCode.BadGateway)
            .Build();


        private readonly IRequestMessage _request = new ServiceRequest.Builder()
            .Url(new Uri("http://totally.a/service"))
            .Headers(new[] { ("a", "header") })
            .Method(HttpMethod.Get)
            .Build();

        private readonly IRequestMessage _transformedRequest = new ServiceRequest.Builder()
            .Url(new Uri("http://totally.a/service"))
            .Headers(new[] { ("another", "header") })
            .Method(HttpMethod.Get)
            .Build();

        private readonly IEnumerable<IInteraction.Note> _note = new[] { new IInteraction.Note(IInteraction.Note.NoteType.Text, "Note", "worthy") };

        public InteractionRecordingServirtiumRequestHandlerTest()
        {
            _mockMonitor.Setup(m => m.GetServiceResponseForRequest(It.IsAny<int>(), It.IsAny<IRequestMessage>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(_response));
            _mockMonitor.Setup(m => m.NoteCompletedInteraction(It.IsAny<int>(), It.IsAny<IRequestMessage>(), It.IsAny<IResponseMessage>(), It.IsAny<IEnumerable<IInteraction.Note>>()));
            _mockMonitor.Setup(m => m.FinishedScript(It.IsAny<int>(), It.IsAny<bool>()));

            _mockTransforms.Setup(t => t.TransformClientRequestForRealService(It.IsAny<IRequestMessage>())).Returns<IRequestMessage>(r => _transformedRequest);
            _mockTransforms.Setup(t => t.TransformRealServiceResponseForClient(It.IsAny<IResponseMessage>())).Returns<IResponseMessage>(r => _transformedResponse);
        }

        [Fact]
        public void ProcessRequest_Always_TransformsRequest()
        {
            new InteractionRecordingServirtiumRequestHandler(_mockTransforms.Object, _mockMonitor.Object)
                .ProcessRequest(_request, _note).Wait();
            _mockTransforms.Verify(t => t.TransformClientRequestForRealService(_request));
        }

        [Fact]
        public void ProcessRequest_Always_SendsTransformedRequestToMonitorToGetResponse()
        {
            new InteractionRecordingServirtiumRequestHandler(_mockTransforms.Object, _mockMonitor.Object)
                .ProcessRequest(_request, _note).Wait();
            _mockMonitor.Verify(m => m.GetServiceResponseForRequest(0, _transformedRequest, false));
        }

        [Fact]
        public void ProcessRequest_MultipleCalls_InteractionNumberSendToMonitorCountsUpFromZero()
        {
            var handler = new InteractionRecordingServirtiumRequestHandler(_mockTransforms.Object, _mockMonitor.Object);
            handler.ProcessRequest(_request, _note).Wait();
            _mockMonitor.Verify(m => m.GetServiceResponseForRequest(0, _transformedRequest, false));
            handler.ProcessRequest(_request, _note).Wait();
            _mockMonitor.Verify(m => m.GetServiceResponseForRequest(1, _transformedRequest, false));
            handler.ProcessRequest(_request, _note).Wait();
            _mockMonitor.Verify(m => m.GetServiceResponseForRequest(2, _transformedRequest, false));
        }

        [Fact]
        public void ProcessRequest_Always_TransformsResponse()
        {
            new InteractionRecordingServirtiumRequestHandler(_mockTransforms.Object, _mockMonitor.Object)
                .ProcessRequest(_request, _note).Wait();
            _mockTransforms.Verify(t => t.TransformRealServiceResponseForClient(_response));
        }

        [Fact]
        public void ProcessRequest_Always_NotesTransformedRequestResponseAndNotesAgainstCurrentInteractionNumber()
        {
            new InteractionRecordingServirtiumRequestHandler(_mockTransforms.Object, _mockMonitor.Object)
                .ProcessRequest(_request, _note).Wait();

            var handler = new InteractionRecordingServirtiumRequestHandler(_mockTransforms.Object, _mockMonitor.Object);
            handler.ProcessRequest(_request, _note).Wait();
            _mockMonitor.Verify(m => m.NoteCompletedInteraction(0, _transformedRequest, _transformedResponse, _note));
            handler.ProcessRequest(_request, _note).Wait();
            _mockMonitor.Verify(m => m.NoteCompletedInteraction(1, _transformedRequest, _transformedResponse, _note));
            handler.ProcessRequest(_request, _note).Wait();
            _mockMonitor.Verify(m => m.NoteCompletedInteraction(2, _transformedRequest, _transformedResponse, _note));
        }

        [Fact]
        public void ProcessRequest_Always_ReturnsResponse()
        {
            Assert.Equal(_transformedResponse, new InteractionRecordingServirtiumRequestHandler(_mockTransforms.Object, _mockMonitor.Object)
                .ProcessRequest(_request, _note).Result);
        }



        [Fact]
        public void FinishedScipt_Always_DelegatesToMonitorFinishedScriptWithCurrentInteractionNumbber()
        {
            var handler = new InteractionRecordingServirtiumRequestHandler(_mockTransforms.Object, _mockMonitor.Object);
            handler.ProcessRequest(_request, _note).Wait();
            handler.ProcessRequest(_request, _note).Wait();
            handler.ProcessRequest(_request, _note).Wait();
            handler.ProcessRequest(_request, _note).Wait();
            handler.ProcessRequest(_request, _note).Wait();
            handler.FinishedScript();
            _mockMonitor.Verify(m => m.FinishedScript(4, It.IsAny<bool>()));
        }
    }
}
