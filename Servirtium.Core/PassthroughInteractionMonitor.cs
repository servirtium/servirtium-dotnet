using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Servirtium.Core
{
    public class PassThroughInteractionMonitor : IInteractionMonitor
    {
        private readonly IServiceInteroperation _service;
        private readonly Uri _redirectHost;

        public PassThroughInteractionMonitor(Uri redirectHost) : this(redirectHost, new ServiceInteropViaSystemNetHttp()) { }

        public PassThroughInteractionMonitor(Uri redirectHost, IServiceInteroperation service)
        {
            _redirectHost = redirectHost;
            _service = service;
        }


        public async Task<IResponseMessage> GetServiceResponseForRequest(Uri host, IInteraction interaction, bool lowerCaseHeaders)
        {
            return await _service.InvokeServiceEndpoint(
                interaction.Method, 
                interaction.HasRequestBody ? interaction.RequestBody : null,
                interaction.HasRequestBody ? interaction.RequestContentType : null,
                new Uri($"{_redirectHost.GetLeftPart(UriPartial.Authority)}{interaction.Path}"),
                interaction.RequestHeaders);
        }
    }
}
