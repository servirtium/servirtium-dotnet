using System;
using System.Collections.Generic;
using System.Text;

namespace Servirtium.Core
{
    public interface IInteractionTransforms
    {

        IRequestMessage TransformClientRequestForRealService(IRequestMessage clientRequest) => clientRequest;

        IResponseMessage TransformRealServiceResponseForClient(IResponseMessage serviceResponse) => serviceResponse;

    }
}
