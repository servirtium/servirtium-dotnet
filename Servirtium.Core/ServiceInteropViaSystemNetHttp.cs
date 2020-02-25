using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Servirtium.Core
{
    public class ServiceInteropViaSystemNetHttp : IServiceInteroperation
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
            if (clientRequestBody!=null && clientRequestContentType !=null)
            {
                request.Content = new StringContent(clientRequestBody.ToString());
                request.Content.Headers.ContentType = clientRequestContentType;
            }
            foreach((string name, string value) in clientRequestHeaders)
            {
                var lowerCaseName = name.ToLower();
                if (lowerCaseName != "content-type")
                {
                    if (lowerCaseName.StartsWith("content"))
                    {
                        request.Content?.Headers.Add(name, value);
                    }
                    else
                    {
                        request.Headers.Add(name, value);
                    }
                }
            }
            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsByteArrayAsync();
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
