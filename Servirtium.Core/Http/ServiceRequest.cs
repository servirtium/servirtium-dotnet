using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Servirtium.Core.Http
{
    public class ServiceRequest : IRequestMessage
    {
        public ServiceRequest(Uri url, HttpMethod method, IEnumerable<(string Name, string Value)> headers, byte[]? body, MediaTypeHeaderValue? contentType)
        {
            Url = url;
            Method = method;
            Headers = headers;
            Body = body;
            ContentType = contentType;
        }

        public Uri Url { get; }

        public HttpMethod Method { get; }

        public IEnumerable<(string Name, string Value)> Headers { get; }

        public byte[]? Body { get; }

        public MediaTypeHeaderValue? ContentType { get; }

        public class Builder
        {
            private Uri _url = new Uri("http://localhost");
            private IEnumerable<(string Name, string Value)> _headers = new (string Name, string Value)[0];
            private byte[]? _body;
            private MediaTypeHeaderValue? _contentType;
            private HttpMethod _method = HttpMethod.Get;

            public Builder Url(Uri url)
            {
                _url = url;
                return this;
            }

            public Builder Headers(IEnumerable<(string, string)> headers)
            {
                _headers = headers;
                return this;
            }
            public Builder Headers(params (string, string)[] headers) => Headers((IEnumerable<(string, string)>)headers);

            public Builder Method(HttpMethod method)
            {
                _method = method;
                return this;
            }

            public Builder Body(string body, MediaTypeHeaderValue contentType, bool createContentLengthHeader = false) => Body(Encoding.UTF8.GetBytes(body), contentType, createContentLengthHeader);

            public Builder Body(byte[] body, MediaTypeHeaderValue contentType, bool createContentLengthHeader = false)
            {
                _headers = IHttpMessage.FixContentLengthHeader(_headers, body, createContentLengthHeader);
                _body = body;
                _contentType = contentType;
                return this;
            }

            public Builder FixContentLength()
            {
                if (_body != null)
                {
                    _headers = IHttpMessage.FixContentLengthHeader(_headers, _body, false);
                }
                return this;
            }

            public Builder From(IRequestMessage message)
            {
                _body = message.Body;
                _contentType = message.ContentType;
                _headers = message.Headers;
                _method = message.Method;
                _url = message.Url;
                return this;
            }

            public ServiceRequest Build()
            {
                return new ServiceRequest(_url, _method, _headers, _body, _contentType);
            }
        }
    }
}
