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

        private readonly IBodyFormatter _requestBodyFormatter = new TextAndBinaryBodyFormatter(new UTF8TextBodyFormatter(), new Base64BinaryBodyFormatter())
            , _responseBodyFormatter = new TextAndBinaryBodyFormatter(new UTF8TextBodyFormatter(), new Base64BinaryBodyFormatter());

        public InteractionRecorder(Uri redirectHost, string targetFile, IScriptWriter scriptWriter) : this(redirectHost, targetFile, scriptWriter, new ServiceInteropViaSystemNetHttp()) { }

        public InteractionRecorder(Uri redirectHost, string targetFile, IScriptWriter scriptWriter, IServiceInteroperation service, IDictionary<int, IInteraction>? interactions = null)
            : this(redirectHost, () => File.CreateText(targetFile), scriptWriter, service, interactions)
        { }

        public InteractionRecorder(Uri redirectHost, Func<TextWriter> outputWriterFactory, IScriptWriter scriptWriter, IServiceInteroperation service, IDictionary<int, IInteraction>? interactions = null)
        {
            _redirectHost = redirectHost;
            _service = service;
            _scriptWriter = scriptWriter;
            _allInteractions = interactions ?? new Dictionary<int, IInteraction> { };
            _writerFactory = outputWriterFactory;
        }

        public async Task<IResponseMessage> GetServiceResponseForRequest(Uri host, IInteraction interaction, bool lowerCaseHeaders = false)
        {
            var response = await _service.InvokeServiceEndpoint(
                interaction.Method, interaction.RequestBody, interaction.RequestContentType,
                new Uri($"{_redirectHost.GetLeftPart(UriPartial.Authority)}{interaction.Path}"),
                interaction.RequestHeaders);
            return response;
        }

        public void NoteCompletedInteraction(IInteraction requestInteraction, IResponseMessage responseFromService) 
        {
            var builder = new ImmutableInteraction.Builder()
             .From(requestInteraction)
             .ResponseHeaders(responseFromService.Headers)
             .StatusCode(responseFromService.StatusCode);

            if (responseFromService.Body != null && responseFromService.ContentType != null)
            {
                builder.ResponseBody(_responseBodyFormatter.Write(responseFromService.Body, responseFromService.ContentType), responseFromService.ContentType);
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
