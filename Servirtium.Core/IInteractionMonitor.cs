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

    /**
     * Set the filename for the source of the conversation
     * @param filename the filename
     */
        void SetScriptFilename(String filename) { }

        Task<ServiceResponse> GetServiceResponseForRequest(string method, Uri url,
                                                     IInteraction interaction,
                                                     bool lowerCaseHeaders);

        IInteraction NewInteraction(int interactionNum, String context, String method, String path, String url);

        void CodeNoteForNextInteraction(String title, String multiline) { }

        void NoteForNextInteraction(String title, String multiline) { }

    }
}
