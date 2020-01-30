using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Servirtium.Core
{
    class ServiceInteropViaSystemNetHttp : IServiceInteroperation
    {
        private readonly HttpClient _httpClient;

        public ServiceInteropViaSystemNetHttp(): this(new HttpClient()) { }

        public ServiceInteropViaSystemNetHttp(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ServiceResponse> InvokeServiceEndpoint(HttpMethod method, object? clientRequestBody, MediaTypeHeaderValue? clientRequestContentType, Uri url, IEnumerable<(string, string)> clientRequestHeaders)
        {
            var request = new HttpRequestMessage(method, url);
            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            return new ServiceResponse(
                body,
                response.Content.Headers.ContentType,
                response.StatusCode,
                response.Headers
                    .SelectMany(h =>
                        h.Value.Select(v => (h.Key, v))
                    ).Append(("Content-Length", body.Length.ToString())).ToArray()
                );
        }
    }
}
