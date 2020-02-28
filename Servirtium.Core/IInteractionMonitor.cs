using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Servirtium.Core
{
    public interface IInteractionMonitor
    {
        void FinishedScript(int interactionNum, bool failed) { }

        Task<IResponseMessage> GetServiceResponseForRequest(int interactionNumber, IRequestMessage request,
                                                     bool lowerCaseHeaders = false);

        void NoteCompletedInteraction(int interactionNumber, IRequestMessage request, IResponseMessage responseFromService, ICollection<IInteraction.Note> notes) { }

        void CodeNoteForNextInteraction(String title, String multiline) { }

        void NoteForNextInteraction(String title, String multiline) { }

    }
}
