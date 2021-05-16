using Servirtium.Core.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Servirtium.Core.Http
{
    public class InteractionRecordingServirtiumRequestHandler : IServirtiumRequestHandler
    {
        private readonly ILogger<InteractionRecordingServirtiumRequestHandler> _logger;
        
        private readonly InteractionCounter _counter = new InteractionCounter();

        private readonly IHttpMessageTransforms _transforms;

        private readonly IInteractionMonitor _monitor;

        public InteractionRecordingServirtiumRequestHandler(IHttpMessageTransforms transforms, IInteractionMonitor monitor, ILoggerFactory? loggerFactory = null)
        {
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<InteractionRecordingServirtiumRequestHandler>();
            _transforms = transforms;
            _monitor = monitor;
        }

        public async Task<IResponseMessage> ProcessRequest(IRequestMessage request, IEnumerable<IInteraction.Note> notes)
        {
            var interactionNumber = _counter.Bump();
            var serviceRequest = _transforms.TransformClientRequestForRealService(request);
            _logger.LogDebug($"Request for interaction {interactionNumber} transformed for service - {request.Method}: {request.Url} ");
            var responseFromService = await _monitor.GetServiceResponseForRequest(
                interactionNumber,
                serviceRequest,
                false);
            _logger.LogDebug($"Response for interaction {interactionNumber} retrieved - {request.Method}: {request.Url} (Status: {responseFromService.StatusCode})");
            var clientResponse = _transforms.TransformRealServiceResponseForClient(responseFromService);
            _logger.LogDebug($"Response for interaction {interactionNumber} transformed - {request.Method}: {request.Url} (Status: {responseFromService.StatusCode})");
            _monitor.NoteCompletedInteraction(interactionNumber, serviceRequest, clientResponse, notes);
            return clientResponse;
        }

        public void StartScript()
        {
            _monitor.StartScript();
        }

        public void FinishedScript()
        {
            _monitor.FinishedScript(_counter.Get(), false);
        }
    }
}
