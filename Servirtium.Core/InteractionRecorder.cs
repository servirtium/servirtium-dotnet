using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Servirtium.Core
{
    public class InteractionRecorder : IInteractionMonitor
    {
        private readonly IServiceInteroperation _service;
        private readonly IScriptWriter _scriptWriter;
        private readonly Uri _redirectHost;
        private readonly IDictionary<int, IInteraction> _allInteractions;
        private readonly Func<TextWriter> _writerFactory;

        private readonly IBodyFormatter _requestBodyFormatter;
        private readonly IBodyFormatter _responseBodyFormatter;

        public InteractionRecorder(Uri redirectHost, string targetFile, IScriptWriter scriptWriter) : this(redirectHost, targetFile, scriptWriter, new ServiceInteropViaSystemNetHttp()) { }

        public InteractionRecorder(Uri redirectHost, string targetFile, IScriptWriter scriptWriter, IServiceInteroperation service, IDictionary<int, IInteraction>? interactions = null)
            : this(redirectHost, () => File.CreateText(targetFile), scriptWriter, service, interactions, null, null)
        { }

        public InteractionRecorder(Uri redirectHost, Func<TextWriter> outputWriterFactory, IScriptWriter scriptWriter, IServiceInteroperation service, IDictionary<int, IInteraction>? interactions = null, IBodyFormatter? responseBodyFormatter = null, IBodyFormatter? requestBodyFormatter = null)
        {
            _redirectHost = redirectHost;
            _service = service;
            _scriptWriter = scriptWriter;
            _allInteractions = interactions ?? new Dictionary<int, IInteraction> { };
            _writerFactory = outputWriterFactory;
            _requestBodyFormatter = requestBodyFormatter ?? new TextAndBinaryBodyFormatter(new UTF8TextBodyFormatter(), new Base64BinaryBodyFormatter());
            _responseBodyFormatter = responseBodyFormatter ?? new TextAndBinaryBodyFormatter(new UTF8TextBodyFormatter(), new Base64BinaryBodyFormatter());
        }

        public async Task<IResponseMessage> GetServiceResponseForRequest(int interactionNumber, IRequestMessage request, bool lowerCaseHeaders = false)
        {
            var response = await _service.InvokeServiceEndpoint(
                new ServiceRequest.Builder()
                    .From(request)
                    .Url(new Uri($"{_redirectHost.GetLeftPart(UriPartial.Authority)}{request.Url.PathAndQuery}"))
                    .Build());
            return response;
        }

        public void NoteCompletedInteraction(int interactionNumber, IRequestMessage request, IResponseMessage responseFromService, ICollection<IInteraction.Note> notes) 
        {
            var builder = new ImmutableInteraction.Builder()
                .Number(interactionNumber)
                .Method(request.Method)
                .Path(request.Url.PathAndQuery)
                .RequestHeaders(request.Headers)
                .ResponseHeaders(responseFromService.Headers)
                .StatusCode(responseFromService.StatusCode)
                .Notes(notes);

            if (request.HasBody)
            {
                builder.RequestBody(_requestBodyFormatter.Write(request.Body!, request.ContentType!), request.ContentType!);
            }
            else
            {
                builder.RemoveRequestBody();
            }
            if (responseFromService.HasBody)
            {
                builder.ResponseBody(_responseBodyFormatter.Write(responseFromService.Body!, responseFromService.ContentType!), responseFromService.ContentType!);
            }
            else
            {
                builder.RemoveResponseBody();
            }
            var interactionToRecord = builder.Build();
            _allInteractions.Add(interactionToRecord.Number, interactionToRecord);
        }

        public void FinishedScript(int interactionNum, bool failed)
        {

            using (var fs = _writerFactory())
            {
                _scriptWriter.Write(fs, _allInteractions);
            }
        }
    }
}
