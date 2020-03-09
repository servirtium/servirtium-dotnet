using Microsoft.Extensions.Logging;
using Servirtium.Core.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Servirtium.Core.Interactions
{
    public class PassThroughInteractionMonitor : IInteractionMonitor
    {
        private readonly IServiceInteroperation _service;
        private readonly Uri? _redirectHost;

        public PassThroughInteractionMonitor(bool bypassProxy = false, ILoggerFactory? loggerFactory = null) : this(null, new ServiceInteropViaSystemNetHttp(bypassProxy, loggerFactory)) { }
        public PassThroughInteractionMonitor(Uri redirectHost, ILoggerFactory? loggerFactory = null) : this(redirectHost, new ServiceInteropViaSystemNetHttp(false, loggerFactory)) { }

        public PassThroughInteractionMonitor(Uri? redirectHost, IServiceInteroperation service)
        {
            _redirectHost = redirectHost;
            _service = service;
        }


        public async Task<IResponseMessage> GetServiceResponseForRequest(int interactionNumber, IRequestMessage request, bool lowerCaseHeaders)
        {
            return await _service.InvokeServiceEndpoint(
                new ServiceRequest.Builder()
                    .From(request)
                    .Url(_redirectHost != null ? 
                        new Uri($"{_redirectHost.GetLeftPart(UriPartial.Authority)}{request.Url.PathAndQuery}") : request.Url)
                    .Build());
        }
    }
}
