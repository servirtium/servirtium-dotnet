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


        private ServiceResponse createServiceResponse() => 
            new ServiceResponse(Encoding.UTF8.GetBytes("The response body"), MediaTypeHeaderValue.Parse("text/plain"), HttpStatusCode.OK, ("response-header", "value"), ("another response-header", "another value"));

        private readonly Mock<IScriptWriter> _mockScriptWriter;
        private readonly Mock<IServiceInteroperation> _mockServiceInterop;
        private readonly Mock<IInteraction> _mockValidRequestInteraction;
        private readonly Mock<TextWriter> _mockWriter;

        IDictionary<int, IInteraction>? _capturedInteractions;

        private InteractionRecorder createRecorderToTest()=> new InteractionRecorder(_redirectHost, ()=>_mockWriter.Object, _mockScriptWriter.Object, _mockServiceInterop.Object);

        private void AddBodyToRequest() 
        {
            _mockValidRequestInteraction.Setup(i => i.Method).Returns(HttpMethod.Post);
            _mockValidRequestInteraction.Setup(i => i.RequestBody).Returns("The request body.");
            _mockValidRequestInteraction.Setup(i => i.RequestContentType).Returns(MediaTypeHeaderValue.Parse("text/html"));
        }

        public InteractionRecorderTest()
        {
            _mockScriptWriter = new Mock<IScriptWriter>();
            
            _mockServiceInterop = new Mock<IServiceInteroperation>();
            _mockServiceInterop.Setup(s => s.InvokeServiceEndpoint(It.IsAny<HttpMethod>(), It.IsAny<object?>(), It.IsAny<MediaTypeHeaderValue?>(), It.IsAny<Uri>(), It.IsAny<IEnumerable<(string, string)>>()))
                .Returns(Task.FromResult(createServiceResponse()));

            _mockValidRequestInteraction = new Mock<IInteraction>();
            _mockValidRequestInteraction.Setup(i => i.Number).Returns(1337);
            _mockValidRequestInteraction.Setup(i => i.Path).Returns("/mock/request/path");
            _mockValidRequestInteraction.Setup(i => i.Method).Returns(HttpMethod.Get);
            _mockValidRequestInteraction.Setup(i => i.RequestHeaders).Returns(_requestHeaders);

            
            _mockScriptWriter.Setup(sw => sw.Write(It.IsAny<TextWriter>(), It.IsAny<IDictionary<int, IInteraction>>())).Callback<TextWriter, IDictionary<int, IInteraction>>((tw, interactions) => _capturedInteractions = interactions);

            _mockWriter = new Mock<TextWriter>();

            new ServiceResponse(Encoding.UTF8.GetBytes("The response body"), MediaTypeHeaderValue.Parse("text/plain"), HttpStatusCode.OK, ("response-header", "value"), ("another response-header", "another value"));

        }

        [Fact]
        public void GetServiceResponseForRequest_RequestWithoutBody_CallsInteropWithRequestRedirectedToServiceHost()
        {
            var response = createRecorderToTest().GetServiceResponseForRequest(_requestHost, _mockValidRequestInteraction.Object).Result;
            _mockServiceInterop.Verify(s => s.InvokeServiceEndpoint(
                HttpMethod.Get,
                null,
                null,
                new Uri($"{_redirectHost}mock/request/path"),
                _requestHeaders
            ));
        }

        [Fact]
        public void GetServiceResponseForRequest_RequestWithBody_CallsInteropWithRequestWithBody()
        {
            AddBodyToRequest();
            var response = createRecorderToTest().GetServiceResponseForRequest(_requestHost, _mockValidRequestInteraction.Object).Result;
            _mockServiceInterop.Verify(s => s.InvokeServiceEndpoint(
                HttpMethod.Post,
                "The request body.",
                MediaTypeHeaderValue.Parse("text/html"),
                new Uri($"{_redirectHost}mock/request/path"),
                _requestHeaders
            ));
        }

        [Fact]
        public void NoteCompletedInteraction_ValidRequestAndResponse_DoesNotThrow()
        {
            createRecorderToTest().NoteCompletedInteraction(_mockValidRequestInteraction.Object, createServiceResponse());
        }

        [Fact]
        public void NoteCompletedInteraction_RequestWithBodyAndResponse_DoesNotThrow()
        {
            AddBodyToRequest();
            createRecorderToTest().NoteCompletedInteraction(_mockValidRequestInteraction.Object, createServiceResponse());
        }

        [Fact]
        public void NoteCompletedInteraction_InteractionWithDuplicateNumber_Throws()
        {
            AddBodyToRequest();
            var recorder = createRecorderToTest();
            recorder.NoteCompletedInteraction(_mockValidRequestInteraction.Object, createServiceResponse());
            var dup = new ImmutableInteraction.Builder()
                .From(_mockValidRequestInteraction.Object)
                .Path("not/everything/the/same")
                .Build();
            Assert.ThrowsAny<Exception>(()=> recorder.NoteCompletedInteraction(dup, createServiceResponse()));
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
            recorder.NoteCompletedInteraction(_mockValidRequestInteraction.Object, createServiceResponse());
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
        public void FinishedScript_FailedTrue_NoEffect()
        {
            var recorder = createRecorderToTest();
            recorder.NoteCompletedInteraction(_mockValidRequestInteraction.Object, createServiceResponse());
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
            recorder.NoteCompletedInteraction(_mockValidRequestInteraction.Object, serviceResponse);
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
            _mockValidRequestInteraction.Setup(s => s.Notes).Returns(new[] {
                new IInteraction.Note(IInteraction.Note.NoteType.Text, "A Note", "Hello!"),
                new IInteraction.Note(IInteraction.Note.NoteType.Code, "Some code", "Hello world!")
            });
            var recorder = createRecorderToTest();
            recorder.NoteCompletedInteraction(_mockValidRequestInteraction.Object, createServiceResponse());
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
            recorder.NoteCompletedInteraction(_mockValidRequestInteraction.Object, createServiceResponse());
            var second = new ImmutableInteraction.Builder().From(_mockValidRequestInteraction.Object)
                .Number(666)
                .Build();
            recorder.NoteCompletedInteraction(second, createServiceResponse());
            var third = new ImmutableInteraction.Builder().From(_mockValidRequestInteraction.Object)
                .Number(42)
                .Build();
            recorder.NoteCompletedInteraction(third, createServiceResponse());
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
            recorder.NoteCompletedInteraction(_mockValidRequestInteraction.Object, createServiceResponse());
            recorder.FinishedScript(1, false);
            _mockWriter.Protected().Verify("Dispose", Times.Once(), true, true);
        }

        [Fact]
        public void FinishedScript_ThrowsWritingScript_DisposesTextWriter()
        {
            _mockWriter.Protected().Setup("Dispose", true, It.IsAny<bool>());
            _mockScriptWriter.Setup(sw => sw.Write(It.IsAny<TextWriter>(), It.IsAny<IDictionary<int, IInteraction>>())).Throws(new Exception("Barf!"));
            var recorder = createRecorderToTest();
            recorder.NoteCompletedInteraction(_mockValidRequestInteraction.Object, createServiceResponse());
            Assert.Throws<Exception>(()=> recorder.FinishedScript(1, false));
            _mockWriter.Protected().Verify("Dispose", Times.Once(), true, true);
        }
    }
}
