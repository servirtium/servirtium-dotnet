using Servirtium.Core.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Servirtium.Core.Interactions
{
    public class InteractionRecorder : IInteractionMonitor
    {
        private readonly ILogger<InteractionRecorder> _logger;
        private readonly IServiceInteroperation _service;
        private readonly IScriptWriter _scriptWriter;
        private readonly Uri? _redirectHost;
        private readonly IDictionary<int, IInteraction> _allInteractions;
        private readonly Func<TextWriter> _writerFactory;

        private readonly IBodyFormatter _requestBodyFormatter;
        private readonly IBodyFormatter _responseBodyFormatter;
        
        public InteractionRecorder(string targetFile, IScriptWriter scriptWriter, bool bypassProxy = false, ILoggerFactory? loggerFactory = null) : this(null, () => File.CreateText(targetFile), scriptWriter, new ServiceInteropViaSystemNetHttp(bypassProxy, loggerFactory)) { }

        public InteractionRecorder(Uri redirectHost, string targetFile, IScriptWriter scriptWriter, ILoggerFactory? loggerFactory = null) : this(redirectHost, targetFile, scriptWriter, new ServiceInteropViaSystemNetHttp(false, loggerFactory)) { }

        public InteractionRecorder(Uri redirectHost, string targetFile, IScriptWriter scriptWriter, IServiceInteroperation service, IDictionary<int, IInteraction>? interactions = null, ILoggerFactory? loggerFactory = null)
            : this(redirectHost, () => File.CreateText(targetFile), scriptWriter, service, interactions, null, null, loggerFactory)
        { }

        public InteractionRecorder(Uri? redirectHost, Func<TextWriter> outputWriterFactory, IScriptWriter scriptWriter, IServiceInteroperation service, IDictionary<int, IInteraction>? interactions = null, IBodyFormatter? responseBodyFormatter = null, IBodyFormatter? requestBodyFormatter = null, ILoggerFactory? loggerFactory = null)
        {
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<InteractionRecorder>();
            _redirectHost = redirectHost;
            _service = service;
            _scriptWriter = scriptWriter;
            _allInteractions = interactions ?? new Dictionary<int, IInteraction> { };
            _writerFactory = outputWriterFactory;
            _requestBodyFormatter = requestBodyFormatter ?? new TextAndBinaryBodyFormatter(new UTF8TextBodyFormatter(), new Base64BinaryBodyFormatter(), loggerFactory);
            _responseBodyFormatter = responseBodyFormatter ?? new TextAndBinaryBodyFormatter(new UTF8TextBodyFormatter(), new Base64BinaryBodyFormatter(), loggerFactory);
        }

        public async Task<IResponseMessage> GetServiceResponseForRequest(int interactionNumber, IRequestMessage request, bool lowerCaseHeaders = false)
        {
            var requestBuilder = new ServiceRequest.Builder()
                .From(request);
            
            if (_redirectHost!=null)
            {
                requestBuilder.Url(
                    new Uri($"{_redirectHost.GetLeftPart(UriPartial.Authority)}{request.Url.PathAndQuery}"));
            }
            return await _service.InvokeServiceEndpoint(requestBuilder.Build());
        }

        public void NoteCompletedInteraction(int interactionNumber, IRequestMessage request, IResponseMessage responseFromService, IEnumerable<IInteraction.Note> notes) 
        {
            var builder = new ImmutableInteraction.Builder()
                .Number(interactionNumber)
                .Method(request.Method)
                .Path(request.Url.PathAndQuery)
                .RequestHeaders(request.Headers)
                .ResponseHeaders(responseFromService.Headers)
                .StatusCode(responseFromService.StatusCode)
                .Notes(notes);

            if (request.Body!=null)
            {
                builder.RequestBody(_requestBodyFormatter.Write(request.Body.Value.Content, request.Body.Value.Type), request.Body.Value.Type);
            }
            else
            {
                builder.RemoveRequestBody();
            }
            if (responseFromService.Body!=null)
            {
                builder.ResponseBody(_responseBodyFormatter.Write(responseFromService.Body.Value.Content, responseFromService.Body.Value.Type), responseFromService.Body.Value.Type);
            }
            else
            {
                builder.RemoveResponseBody();
            }
            var interactionToRecord = builder.Build();
            _logger.LogDebug($"Noting interaction {interactionToRecord.Number}.");
            _allInteractions.Add(interactionToRecord.Number, interactionToRecord);
        }

        public void FinishedScript(int interactionNum, bool failed)
        {
            _logger.LogDebug($"Finishing script, writing {_allInteractions.Count} interactions.");
            using (var fs = _writerFactory())
            {
                _scriptWriter.Write(fs, _allInteractions);
            }
            _logger.LogInformation($"Finished script, wrote {_allInteractions.Count} interactions.");
        }
    }
}
