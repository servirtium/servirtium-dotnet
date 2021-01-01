using Servirtium.Core.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;
using System.Collections.Concurrent;

namespace Servirtium.Core.Interactions
{
    public class InteractionRecorder : IInteractionMonitor
    {
        private enum InteractionState
        {
            Pending, Recorded
        }

        public enum RecordTime
        {
            AfterEachInteraction,
            AfterScriptFinishes
        }

        private readonly ILogger<InteractionRecorder> _logger;
        private readonly IServiceInteroperation _service;
        private readonly IScriptWriter _scriptWriter;
        private readonly Uri? _redirectHost;
        private readonly ConcurrentDictionary<int, (IInteraction interaction, InteractionState state)> _allInteractions;
        private readonly Func<TextWriter> _writerFactory;

        private readonly IBodyFormatter _requestBodyFormatter;
        private readonly IBodyFormatter _responseBodyFormatter;

        private readonly RecordTime _recordTime;
        
        public InteractionRecorder(string targetFile, IScriptWriter scriptWriter, bool bypassProxy = false, ILoggerFactory? loggerFactory = null, RecordTime recordTime = RecordTime.AfterScriptFinishes)
            : this(null, () => File.AppendText(targetFile), scriptWriter, new ServiceInteropViaSystemNetHttp(bypassProxy, loggerFactory), null, null, null, null, recordTime)
        {
            //Ensure file is blank first
            using (File.CreateText(targetFile)) ;
        }

        public InteractionRecorder(Uri redirectHost, string targetFile, IScriptWriter scriptWriter, ILoggerFactory? loggerFactory = null, RecordTime recordTime = RecordTime.AfterScriptFinishes) 
            : this(redirectHost, () => File.AppendText(targetFile), scriptWriter, new ServiceInteropViaSystemNetHttp(false, loggerFactory), null, null, null, null, recordTime)
        {
            //Ensure file is blank first
            using (File.CreateText(targetFile)) ;
        }

        public InteractionRecorder(Uri redirectHost, string targetFile, IScriptWriter scriptWriter, IServiceInteroperation service, IDictionary<int, IInteraction>? interactions = null, ILoggerFactory? loggerFactory = null, RecordTime recordTime = RecordTime.AfterScriptFinishes)
            : this(redirectHost, () => File.AppendText(targetFile), scriptWriter, service, interactions, null, null, loggerFactory, recordTime)
        {
            //Ensure file is blank first
            using (File.CreateText(targetFile)) ;
        }

        public InteractionRecorder(Uri? redirectHost, Func<TextWriter> outputWriterFactory, IScriptWriter scriptWriter, IServiceInteroperation service, IDictionary<int, IInteraction>? interactions = null, IBodyFormatter? responseBodyFormatter = null, IBodyFormatter? requestBodyFormatter = null, ILoggerFactory? loggerFactory = null, RecordTime recordTime = RecordTime.AfterScriptFinishes)
        {
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<InteractionRecorder>();
            _redirectHost = redirectHost;
            _service = service;
            _scriptWriter = scriptWriter;
            _allInteractions = new ConcurrentDictionary<int, (IInteraction interaction, InteractionState state)>((interactions ?? new Dictionary<int, IInteraction> { }).ToDictionary(kvp=>kvp.Key, kvp=>(kvp.Value, InteractionState.Pending)));
            _writerFactory = outputWriterFactory;
            _requestBodyFormatter = requestBodyFormatter ?? new TextAndBinaryBodyFormatter(new UTF8TextBodyFormatter(), new Base64BinaryBodyFormatter(), loggerFactory);
            _responseBodyFormatter = responseBodyFormatter ?? new TextAndBinaryBodyFormatter(new UTF8TextBodyFormatter(), new Base64BinaryBodyFormatter(), loggerFactory);
            _recordTime = recordTime;
        }

        private void RecordPendingInteractions() 
        {
            lock (_logger)
            {
                var pending = _allInteractions.Where(kvp => kvp.Value.state == InteractionState.Pending)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.interaction);
                using (var fs = _writerFactory())
                {

                    try
                    {
                        _scriptWriter.Write(
                            fs,
                            pending);
                    }
                    catch (MissingInteractionException ex)
                    {
                        foreach (var kvp in pending.Where(kvp => kvp.Value.Number < ex.MissingInteractionNumber)) 
                        {
                            _allInteractions[kvp.Key] = (kvp.Value, InteractionState.Recorded);    
                        }
                        throw;
                    }
                    foreach(var kvp in pending)
                    {
                        _allInteractions[kvp.Key] = (kvp.Value, InteractionState.Recorded);
                    }
                }
            }
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
            if (!_allInteractions.TryAdd(interactionToRecord.Number,(interactionToRecord, InteractionState.Pending)))
            {
                throw new ArgumentException($"Duplicate interaction number: {interactionToRecord.Number}, request: {interactionToRecord.Method} {interactionToRecord.Path}");
            }

            if (_recordTime == RecordTime.AfterEachInteraction)
            {
                _logger.LogDebug($"Recording pending interactions up to {interactionToRecord.Number}.");
                try
                {
                    RecordPendingInteractions();
                }
                catch (MissingInteractionException ex)
                {
                    _logger.LogInformation($"Interaction No. {ex.MissingInteractionNumber} is not available, stopping recording here.");
                }

            }
        }

        public void FinishedScript(int interactionNum, bool failed)
        {
            if (_recordTime == RecordTime.AfterScriptFinishes)
            {
                _logger.LogDebug($"Finishing script, writing {_allInteractions.Count} interactions.");
                RecordPendingInteractions();
                _logger.LogInformation($"Finished script, wrote {_allInteractions.Count} interactions.");
            }
        }
    }
}
