using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Servirtium.Core.Http
{
    public class ServiceInteropViaSystemNetHttp : IServiceInteroperation
    {
        private readonly HttpClient _httpClient;

        public ServiceInteropViaSystemNetHttp(bool bypassProxy = false): this(new HttpClient(new HttpClientHandler{ UseProxy = !bypassProxy})) { }

        public ServiceInteropViaSystemNetHttp(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IResponseMessage> InvokeServiceEndpoint(IRequestMessage requestMessage)
        {
            var request = new HttpRequestMessage(requestMessage.Method, requestMessage.Url);
            if (requestMessage.Body.HasValue)
            {
                request.Content = new ByteArrayContent(requestMessage.Body.Value.Content);
                request.Content.Headers.ContentType = requestMessage.Body.Value.Type;
            }
            foreach((string name, string value) in requestMessage.Headers)
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
            HttpResponseMessage response;
            response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsByteArrayAsync();
            var builder = new ServiceResponse.Builder()
                .StatusCode(response.StatusCode)
                .Headers(response.Headers
                    .SelectMany(h =>
                        h.Value.Select(v => (h.Key, v))
                    ).Append(("Content-Length", body.Length.ToString())).ToArray());
            if (body!=null && body.Length>0 && response.Content.Headers.ContentType!=null)
            {
                builder.Body(body, response.Content.Headers.ContentType);
            }
            return builder.Build();
        }
    }
}
