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

        public ServiceInteropViaSystemNetHttp(bool bypassProxy = false): this(new HttpClient(new HttpClientHandler{ UseProxy = !bypassProxy})) { }

        public ServiceInteropViaSystemNetHttp(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IResponseMessage> InvokeServiceEndpoint(IRequestMessage requestMessage)
        {
            var request = new HttpRequestMessage(requestMessage.Method, requestMessage.Url);
            if (requestMessage.HasBody)
            {
                request.Content = new ByteArrayContent(requestMessage.Body);
                request.Content.Headers.ContentType = requestMessage.ContentType;
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
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception e)
            {
                throw;
            }
            var body = await response.Content.ReadAsByteArrayAsync();
            return new ServiceResponse.Builder()
                .Body(body, response.Content.Headers.ContentType)
                .StatusCode(response.StatusCode)
                .Headers(response.Headers
                    .SelectMany(h =>
                        h.Value.Select(v => (h.Key, v))
                    ).Append(("Content-Length", body.Length.ToString())).ToArray())
                .Build();
        }
    }
}
