using Servirtium.Core.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servirtium.Core.Http
{
    public class InteractionRecordingServirtiumRequestHandler : IServirtiumRequestHandler
    {
        private readonly InteractionCounter _counter = new InteractionCounter();

        private readonly IHttpMessageTransforms _transforms;

        private readonly IInteractionMonitor _monitor;

        public InteractionRecordingServirtiumRequestHandler(IHttpMessageTransforms transforms, IInteractionMonitor monitor)
        {
            _transforms = transforms;
            _monitor = monitor;
        }

        public async Task<IResponseMessage> ProcessRequest(IRequestMessage request, IEnumerable<IInteraction.Note> notes)
        {
            var interactionNumber = _counter.Bump();
            var serviceRequest = _transforms.TransformClientRequestForRealService(request);
            var responseFromService = await _monitor.GetServiceResponseForRequest(
                interactionNumber,
                serviceRequest,
                false);
            var clientResponse = _transforms.TransformRealServiceResponseForClient(responseFromService);
            _monitor.NoteCompletedInteraction(interactionNumber, serviceRequest, clientResponse, notes);
            return clientResponse;
        }

        public void FinishedScript()
        {
            _monitor.FinishedScript(_counter.Get(), false);
        }
    }
}
