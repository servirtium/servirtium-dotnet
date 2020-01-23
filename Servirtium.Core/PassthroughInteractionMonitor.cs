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
        private readonly HttpClient _httpClient;
        private readonly Uri _redirectHost;

        public PassThroughInteractionMonitor(Uri redirectHost) : this(redirectHost, new HttpClient()) { }

        public PassThroughInteractionMonitor(Uri redirectHost, HttpClient httpClient)
        {
            _redirectHost = redirectHost;
            _httpClient = httpClient;
        }


        public async Task<ServiceResponse> GetServiceResponseForRequest(string method, Uri incomingUrl, IInteraction interaction, bool lowerCaseHeaders)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), new Uri($"{_redirectHost.GetLeftPart(UriPartial.Authority)}{incomingUrl.PathAndQuery}"));
            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            return new ServiceResponse(
                body,
                response.Content.Headers.ContentType,
                response.StatusCode,
                response.Headers
                    .SelectMany(h=>
                        h.Value.Select(v=>(h.Key, v))
                    ).Append(("Content-Length", body.Length.ToString())).ToArray()
                );
        }

        public IInteraction NewInteraction(int interactionNum, string context, string method, string path, string url)
        {
            throw new NotImplementedException();
        }
    }
}
