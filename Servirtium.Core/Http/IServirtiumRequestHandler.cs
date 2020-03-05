using Servirtium.Core.Interactions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Servirtium.Core.Http
{
    /// <summary>
    /// Encapsulates the core Servirtium request handling logic
    /// IServirtiumRequestHandler Implementations are injected into IServirtiumServer implementations to allow the implementation specific code to hand over to core Servirtium logic.
    /// </summary>
    public interface IServirtiumRequestHandler
    {
        Task<IResponseMessage> ProcessRequest(IRequestMessage request, IEnumerable<IInteraction.Note> notes);

        void FinishedScript() { }
    }

    public class StubServirtiumRequestHandler : IServirtiumRequestHandler
    {
        public Task<IResponseMessage> ProcessRequest(IRequestMessage request, IEnumerable<IInteraction.Note> notes)
        {
            throw new NotImplementedException();
        }
    }
}
