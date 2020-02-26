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

        Task<IResponseMessage> GetServiceResponseForRequest(Uri host,
                                                     IInteraction interaction,
                                                     bool lowerCaseHeaders = false);

        void NoteCompletedInteraction(IInteraction requestInteraction, IResponseMessage responseFromService) { }

        void CodeNoteForNextInteraction(String title, String multiline) { }

        void NoteForNextInteraction(String title, String multiline) { }

    }
}
