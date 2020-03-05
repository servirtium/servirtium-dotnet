using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Servirtium.Core.Http
{
    public interface IServiceInteroperation
    {
        Task<IResponseMessage> InvokeServiceEndpoint(IRequestMessage request);
    }
}
