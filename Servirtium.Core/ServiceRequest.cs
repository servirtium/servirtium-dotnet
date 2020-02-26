using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Servirtium.Core
{
    public class ServiceRequest : IRequestMessage
    {
        public ServiceRequest(HttpMethod method, IEnumerable<(string Name, string Value)> headers, byte[]? body, MediaTypeHeaderValue? contentType)
        {
            Method = method;
            Headers = headers;
            Body = body;
            ContentType = contentType;
        }

        public HttpMethod Method { get; }

        public IEnumerable<(string Name, string Value)> Headers { get; }

        public byte[]? Body { get; }

        public MediaTypeHeaderValue? ContentType { get; }
    }
}
