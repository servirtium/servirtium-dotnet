﻿using Servirtium.Core.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Servirtium.Core.Interactions
{
    public interface IInteractionMonitor
    {
        void StartScript() {}


        
        void FinishedScript(int interactionNum, bool failed) { }

        Task<IResponseMessage> GetServiceResponseForRequest(int interactionNumber, IRequestMessage request,
                                                     bool lowerCaseHeaders = false);

        void NoteCompletedInteraction(int interactionNumber, IRequestMessage request, IResponseMessage responseFromService, IEnumerable<IInteraction.Note> notes) { }

    }
}
