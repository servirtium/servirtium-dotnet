using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Servirtium.Core.Tests
{
    public class InteractionRecorderTest
    {
        private static readonly IEnumerable<(string, string)> _requestHeaders = new[] { ("header-name", "header-value"), ("another-header-name", "another header-value") };
        private static readonly Uri _redirectHost = new Uri("http://a.mock.service"), _requestHost = new Uri("http://the.servirtuium.service");


        private IResponseMessage createServiceResponse() => 
            new ServiceResponse.Builder()
            .Body("The response body", MediaTypeHeaderValue.Parse("text/plain"))
            .StatusCode(HttpStatusCode.OK)
            .Headers(("response-header", "value"), ("another response-header", "another value"))
            .Build();

        private readonly Mock<IScriptWriter> _mockScriptWriter;
        private readonly Mock<IServiceInteroperation> _mockServiceInterop;
        private readonly Mock<IRequestMessage> _mockValidRequest;
        private readonly Mock<TextWriter> _mockWriter;

        IDictionary<int, IInteraction>? _capturedInteractions;
        IRequestMessage _capturedRequest;

        private InteractionRecorder createRecorderToTest()=> new InteractionRecorder(_redirectHost, ()=>_mockWriter.Object, _mockScriptWriter.Object, _mockServiceInterop.Object);

        private void AddBodyToRequest() 
        {
            _mockValidRequest.Setup(i => i.Method).Returns(HttpMethod.Post);
            _mockValidRequest.Setup(i => i.HasBody).Returns(true);
            _mockValidRequest.Setup(i => i.Body).Returns(Encoding.UTF8.GetBytes("The request body."));
            _mockValidRequest.Setup(i => i.ContentType).Returns(MediaTypeHeaderValue.Parse("text/html"));
        }

        public InteractionRecorderTest()
        {
            _mockScriptWriter = new Mock<IScriptWriter>();
            
            _mockServiceInterop = new Mock<IServiceInteroperation>();
            _mockServiceInterop.Setup(s => s.InvokeServiceEndpoint(It.IsAny<IRequestMessage>()))
                .Returns<IRequestMessage>((rm)=> {
                    _capturedRequest = rm;
                    return Task.FromResult(createServiceResponse());
                    });

            _mockValidRequest = new Mock<IRequestMessage>();
            _mockValidRequest.Setup(i => i.Url).Returns(new Uri("http://the.servirtuium.service/mock/request/path"));
            _mockValidRequest.Setup(i => i.Method).Returns(HttpMethod.Get);
            _mockValidRequest.Setup(i => i.Headers).Returns(_requestHeaders);

            
            _mockScriptWriter.Setup(sw => sw.Write(It.IsAny<TextWriter>(), It.IsAny<IDictionary<int, IInteraction>>())).Callback<TextWriter, IDictionary<int, IInteraction>>((tw, interactions) => _capturedInteractions = interactions);

            _mockWriter = new Mock<TextWriter>();

        }

        [Fact]
        public void GetServiceResponseForRequest_RequestWithoutBody_CallsInteropWithRequestRedirectedToServiceHost()
        {
            var response = createRecorderToTest().GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result;
            _mockServiceInterop.Verify(s => s.InvokeServiceEndpoint(It.IsAny<IRequestMessage>()));
            Assert.False(_capturedRequest.HasBody);
            Assert.Equal(_mockValidRequest.Object.Headers, _capturedRequest.Headers);
            Assert.Equal(_mockValidRequest.Object.Method, _capturedRequest.Method);
            Assert.Equal(new Uri(new Uri("http://a.mock.service"), "/mock/request/path"), _capturedRequest.Url);
        }

        [Fact]
        public void GetServiceResponseForRequest_RequestWithBody_CallsInteropWithRequestWithBody()
        {
            AddBodyToRequest();
            var response = createRecorderToTest().GetServiceResponseForRequest(1337, _mockValidRequest.Object).Result;

            _mockServiceInterop.Verify(s => s.InvokeServiceEndpoint(It.IsAny<IRequestMessage>()));
            Assert.True(_capturedRequest.HasBody);
            Assert.Equal(_mockValidRequest.Object.ContentType, _capturedRequest.ContentType);
            Assert.Equal(_mockValidRequest.Object.Body, _capturedRequest.Body);
            Assert.Equal(_mockValidRequest.Object.Headers, _capturedRequest.Headers);
            Assert.Equal(_mockValidRequest.Object.Method, _capturedRequest.Method);
            Assert.Equal(new Uri(new Uri("http://a.mock.service"), "/mock/request/path"), _capturedRequest.Url);
        }

        [Fact]
        public void NoteCompletedInteraction_ValidRequestAndResponse_DoesNotThrow()
        {
            createRecorderToTest().NoteCompletedInteraction(1337, _mockValidRequest.Object, createServiceResponse(), new IInteraction.Note[0]);
        }

        [Fact]
        public void NoteCompletedInteraction_RequestWithBodyAndResponse_DoesNotThrow()
        {
            AddBodyToRequest();
            createRecorderToTest().NoteCompletedInteraction(1337, _mockValidRequest.Object, createServiceResponse(), new IInteraction.Note[0]);
        }

        [Fact]
        public void NoteCompletedInteraction_InteractionWithDuplicateNumber_Throws()
        {
            AddBodyToRequest();
            var recorder = createRecorderToTest();
            recorder.NoteCompletedInteraction(1337, _mockValidRequest.Object, createServiceResponse(), new IInteraction.Note[0]);
            var dup = new ServiceRequest.Builder()
                .From(_mockValidRequest.Object)
                .Url(new Uri("http://another.service/not/everything/the/same"))
                .Build();
            Assert.ThrowsAny<Exception>(()=> recorder.NoteCompletedInteraction(1337, dup, createServiceResponse(), new IInteraction.Note[0]));
        }

        [Fact]
        public void FinishedScript_NoInteractionsNoted_CallsScriptWriterWithEmptyDictionary()
        {
            var recorder = createRecorderToTest();
            recorder.FinishedScript(1, false);
            _mockScriptWriter.Verify(sw => sw.Write(_mockWriter.Object, It.IsAny<IDictionary<int, IInteraction>>()));
            Assert.Empty(_capturedInteractions);

        }

        [Fact]
        public void FinishedScript_OneInteractionNoted_CallsScriptWriterWithOneInteractionInDictionary()
        {
            var recorder = createRecorderToTest();
            var serviceResponse = createServiceResponse();
            recorder.NoteCompletedInteraction(1337, _mockValidRequest.Object, createServiceResponse(), new IInteraction.Note[0]);
            recorder.FinishedScript(1, false);
            _mockScriptWriter.Verify(sw => sw.Write(_mockWriter.Object, It.IsAny<IDictionary<int, IInteraction>>()));
            Assert.Equal(1, _capturedInteractions!.Count);
            var recorded = _capturedInteractions[1337];
            Assert.Equal(1337, recorded.Number);
            Assert.Equal("/mock/request/path", recorded.Path);
            Assert.Equal(HttpMethod.Get, recorded.Method);
            Assert.Equal(_requestHeaders, recorded.RequestHeaders);
            Assert.False(recorded.HasRequestBody);
            Assert.Equal(HttpStatusCode.OK, recorded.StatusCode);
            Assert.Equal(serviceResponse.Headers, recorded.ResponseHeaders);
            Assert.True(recorded.HasResponseBody);
            var bodyAsString = UTF8Encoding.UTF8.GetString(serviceResponse.Body!);
            Assert.Equal(bodyAsString, recorded.ResponseBody);
            Assert.Equal(serviceResponse.ContentType, recorded.ResponseContentType);

        }

        [Fact]
        public void FinishedScript_OneInteractionNoted_WritesResponseBodyProducedWithInjectedResponseBodyFormatter()
        {
            Mock<IBodyFormatter> mockBodyFormatter = new Mock<IBodyFormatter>();
            mockBodyFormatter.Setup(f => f.Write(It.IsAny<byte[]>(), It.IsAny<MediaTypeHeaderValue>())).Returns("FORMATTED RESPONSE BODY");
            var recorder = new InteractionRecorder(_redirectHost, () => _mockWriter.Object, _mockScriptWriter.Object, _mockServiceInterop.Object, null, mockBodyFormatter.Object);

            var serviceResponse = createServiceResponse();
            recorder.NoteCompletedInteraction(1337, _mockValidRequest.Object, createServiceResponse(), new IInteraction.Note[0]);
            recorder.FinishedScript(1, false);
            mockBodyFormatter.Verify(f => f.Write(
                It.Is<byte[]>(b=>Encoding.UTF8.GetString(b)=="The response body"), 
                MediaTypeHeaderValue.Parse("text/plain")));
            var recorded = _capturedInteractions![1337];
            Assert.Equal("FORMATTED RESPONSE BODY", recorded.ResponseBody);
        }

        [Fact]
        public void FinishedScript_OneInteractionNotedWithRequestBody_WritesRequestBodyProducedWithInjectedRequestBodyFormatter()
        {
            Mock<IBodyFormatter> mockBodyFormatter = new Mock<IBodyFormatter>();
            mockBodyFormatter.Setup(f => f.Write(It.IsAny<byte[]>(), It.IsAny<MediaTypeHeaderValue>())).Returns("FORMATTED REQUEST BODY");
            var recorder = new InteractionRecorder(_redirectHost, () => _mockWriter.Object, _mockScriptWriter.Object, _mockServiceInterop.Object, null, null, mockBodyFormatter.Object);
            var serviceResponse = createServiceResponse();

            AddBodyToRequest();           
            recorder.NoteCompletedInteraction(1337, _mockValidRequest.Object, createServiceResponse(), new IInteraction.Note[0]);
            recorder.FinishedScript(1, false);
            mockBodyFormatter.Verify(f => f.Write(
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == "The request body."),
                MediaTypeHeaderValue.Parse("text/html")));
            var recorded = _capturedInteractions![1337];
            Assert.Equal("FORMATTED REQUEST BODY", recorded.RequestBody);
        }

        [Fact]
        public void FinishedScript_FailedTrue_NoEffect()
        {
            var recorder = createRecorderToTest();
            recorder.NoteCompletedInteraction(1337, _mockValidRequest.Object, createServiceResponse(), new IInteraction.Note[0]);
            recorder.FinishedScript(1, true);
            _mockScriptWriter.Verify(sw => sw.Write(_mockWriter.Object, It.IsAny<IDictionary<int, IInteraction>>()));
            Assert.Equal(1, _capturedInteractions!.Count);
            Assert.True(_capturedInteractions.ContainsKey(1337));

        }

        [Fact]
        public void FinishedScript_OneInteractionWithRequestBodyNoted_CallsScriptWriterWithOneInteractionWithRequestBodyInDictionary()
        {
            AddBodyToRequest();
            var recorder = createRecorderToTest();
            var serviceResponse = createServiceResponse();
            recorder.NoteCompletedInteraction(1337, _mockValidRequest.Object, serviceResponse, new IInteraction.Note[0]);
            recorder.FinishedScript(1, false);
            _mockScriptWriter.Verify(sw => sw.Write(_mockWriter.Object, It.IsAny<IDictionary<int, IInteraction>>()));
            Assert.Equal(1, _capturedInteractions!.Count);
            var recorded = _capturedInteractions[1337];
            Assert.Equal(1337, recorded.Number);
            Assert.Equal("/mock/request/path", recorded.Path);
            Assert.Equal(HttpMethod.Post, recorded.Method);
            Assert.Equal(_requestHeaders, recorded.RequestHeaders);
            Assert.True(recorded.HasRequestBody);
            Assert.Equal("The request body.", recorded.RequestBody);
            Assert.Equal(MediaTypeHeaderValue.Parse("text/html"), recorded.RequestContentType);
            Assert.Equal(HttpStatusCode.OK, recorded.StatusCode);
            Assert.Equal(serviceResponse.Headers, recorded.ResponseHeaders);
            Assert.True(recorded.HasResponseBody);
            var bodyAsString = UTF8Encoding.UTF8.GetString(serviceResponse.Body!);
            Assert.Equal(bodyAsString, recorded.ResponseBody);
            Assert.Equal(serviceResponse.ContentType, recorded.ResponseContentType);
        }

        [Fact]
        public void FinishedScript_OneInteractionWithNotes_NotesAreSentToScriptWriterWithInteraction()
        {
            var recorder = createRecorderToTest();
            recorder.NoteCompletedInteraction(1337, _mockValidRequest.Object, createServiceResponse(), new[] {
                new IInteraction.Note(IInteraction.Note.NoteType.Text, "A Note", "Hello!"),
                new IInteraction.Note(IInteraction.Note.NoteType.Code, "Some code", "Hello world!")
            });
            recorder.FinishedScript(1, false);
            _mockScriptWriter.Verify(sw => sw.Write(_mockWriter.Object, It.IsAny<IDictionary<int, IInteraction>>()));
            Assert.Equal(1, _capturedInteractions!.Count);
            var recorded = _capturedInteractions[1337];
            Assert.Equal(1337, recorded.Number);
            Assert.Equal("/mock/request/path", recorded.Path);
            Assert.Equal(HttpMethod.Get, recorded.Method);
            var notes = recorded.Notes;
            Assert.Equal(2, notes.Count());
            Assert.Equal(IInteraction.Note.NoteType.Text, notes.First().Type);
            Assert.Equal("A Note", notes.First().Title);
            Assert.Equal("Hello!", notes.First().Content);
            Assert.Equal(IInteraction.Note.NoteType.Code, notes.Last().Type);
            Assert.Equal("Some code", notes.Last().Title);
            Assert.Equal("Hello world!", notes.Last().Content);
        }

        [Fact]
        public void FinishedScript_ThreeInteractionsWithDifferentNumbersNoted_CallsScriptWriterWithThreeInteractionInDictionary()
        {
            var recorder = createRecorderToTest();
            recorder.NoteCompletedInteraction(1337, _mockValidRequest.Object, createServiceResponse(), new IInteraction.Note[0]);
            recorder.NoteCompletedInteraction(666, _mockValidRequest.Object, createServiceResponse(), new IInteraction.Note[0]);
            recorder.NoteCompletedInteraction(42, _mockValidRequest.Object, createServiceResponse(), new IInteraction.Note[0]);
            recorder.FinishedScript(1, true);
            _mockScriptWriter.Verify(sw => sw.Write(_mockWriter.Object, It.IsAny<IDictionary<int, IInteraction>>()));
            Assert.Equal(3, _capturedInteractions!.Count);
            Assert.True(_capturedInteractions.ContainsKey(42));
            Assert.True(_capturedInteractions.ContainsKey(666));
            Assert.True(_capturedInteractions.ContainsKey(1337));
        }

        [Fact]
        public void FinishedScript_SuccessfullyWrites_DisposesTextWriter()
        {
            _mockWriter.Protected().Setup("Dispose", true, It.IsAny<bool>());
            var recorder = createRecorderToTest();
            recorder.NoteCompletedInteraction(1337, _mockValidRequest.Object, createServiceResponse(), new IInteraction.Note[0]);
            recorder.FinishedScript(1, false);
            _mockWriter.Protected().Verify("Dispose", Times.Once(), true, true);
        }

        [Fact]
        public void FinishedScript_ThrowsWritingScript_DisposesTextWriter()
        {
            _mockWriter.Protected().Setup("Dispose", true, It.IsAny<bool>());
            _mockScriptWriter.Setup(sw => sw.Write(It.IsAny<TextWriter>(), It.IsAny<IDictionary<int, IInteraction>>())).Throws(new Exception("Barf!"));
            var recorder = createRecorderToTest();
            recorder.NoteCompletedInteraction(1337, _mockValidRequest.Object, createServiceResponse(), new IInteraction.Note[0]);
            Assert.Throws<Exception>(()=> recorder.FinishedScript(1, false));
            _mockWriter.Protected().Verify("Dispose", Times.Once(), true, true);
        }
    }
}
