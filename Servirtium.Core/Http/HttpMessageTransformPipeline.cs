using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Servirtium.Core.Http
{
    public class HttpMessageTransformPipeline : IHttpMessageTransforms
    {
        IEnumerable<IHttpMessageTransforms> _transforms;

        public HttpMessageTransformPipeline(params IHttpMessageTransforms[] transforms)
        {
            _transforms = transforms;
        }


        public IRequestMessage TransformClientRequestForRealService(IRequestMessage clientRequest)
        {
            return  _transforms.Aggregate(clientRequest, (request, transform) => transform.TransformClientRequestForRealService(request));
        }

        public IResponseMessage TransformRealServiceResponseForClient(IResponseMessage serviceResponse)
        {
            return _transforms.Aggregate(serviceResponse, (response, transform) => transform.TransformRealServiceResponseForClient(response));
        }
    }
}
