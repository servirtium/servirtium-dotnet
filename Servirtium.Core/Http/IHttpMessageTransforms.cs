using System;
using System.Collections.Generic;
using System.Text;

namespace Servirtium.Core.Http
{
    public interface IHttpMessageTransforms
    {

        IRequestMessage TransformClientRequestForRealService(IRequestMessage clientRequest) => clientRequest;

        IResponseMessage TransformRealServiceResponseForClient(IResponseMessage serviceResponse) => serviceResponse;

    }
}
