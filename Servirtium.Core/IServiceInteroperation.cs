using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Servirtium.Core
{
    public interface IServiceInteroperation
    {
        Task<ServiceResponse> InvokeServiceEndpoint(HttpMethod method,
                                          object? clientRequestBody,
                                          MediaTypeHeaderValue? clientRequestContentType,
                                          Uri url,
                                          IEnumerable<(string, string)> clientRequestHeaders);
    }
}
