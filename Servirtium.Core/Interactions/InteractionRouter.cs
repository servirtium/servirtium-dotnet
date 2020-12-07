using Servirtium.Core.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Servirtium.Core.Interactions
{
    public class InteractionRouter : IInteractionMonitor
    {
        private readonly IEnumerable<(IInteractionMonitor, Func<IRequestMessage, bool>)> _routes;

        private IInteractionMonitor? _currentDelegate;

        private IInteractionMonitor CurrentDelegate => _currentDelegate ?? throw new InvalidOperationException("The monitor to delegate to must have been set by a GetServiceResponseForRequest call prior to attempting this operation");

        public InteractionRouter(params (IInteractionMonitor, Func<IRequestMessage, bool>)[] routes)
        {
            _routes = routes;
        }

        public async Task<IResponseMessage> GetServiceResponseForRequest(int interactionNumber, IRequestMessage request, bool lowerCaseHeaders = false)
        {
            foreach(var (monitor, validator) in _routes)
            {
                if (validator(request))
                {
                    _currentDelegate = monitor;
                    return await monitor.GetServiceResponseForRequest(interactionNumber, request, lowerCaseHeaders);
                }
            }
            throw new ArgumentException($"Incoming request: {request} could not be routed to any of the {_routes.Count()} registered interaction monitors, none of their validators indicated they were a valid monitor for this request.");
        }

        public static Func<IRequestMessage, bool> UrlPatternValidator(string pattern) 
        {
            var regex = new Regex(pattern);
            return (request) => regex.IsMatch(request.Url.AbsoluteUri);
        }

        public void FinishedScript(int interactionNum, bool failed) => CurrentDelegate.FinishedScript(interactionNum, failed);

        public void NoteCompletedInteraction(int interactionNumber, IRequestMessage request, IResponseMessage responseFromService, IEnumerable<IInteraction.Note> notes) => CurrentDelegate.NoteCompletedInteraction(interactionNumber, request, responseFromService, notes);

        //public void CodeNoteForNextInteraction(String title, String multiline) => CurrentDelegate.CodeNoteForNextInteraction(title, multiline);

        //public void NoteForNextInteraction(String title, String multiline) => CurrentDelegate.NoteForNextInteraction(title, multiline);
    }
}
