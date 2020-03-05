using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Servirtium.Core.Http
{
    public class ServiceResponse : IResponseMessage
    {
        public IEnumerable<(string Name, string Value)> Headers { get; }
        public byte[]? Body { get; }
        public MediaTypeHeaderValue? ContentType { get; }
        public HttpStatusCode StatusCode { get; }

        private ServiceResponse(byte[]? body, MediaTypeHeaderValue? contentType, HttpStatusCode statusCode, IEnumerable<(string, string)> headers)
        {
            Headers = headers;
            Body = body;
            ContentType = contentType;
            StatusCode = statusCode;
        }

        public class Builder
        {
            private IEnumerable<(string Name, string Value)> _headers = new (string Name, string Value)[0];
            private byte[]? _body;
            private MediaTypeHeaderValue? _contentType;
            private HttpStatusCode _statusCode = HttpStatusCode.OK;
            private readonly Func<IEnumerable<(string, string)>, byte[], bool, IEnumerable<(string, string)>> _fixContentLengthHeader;
            public Builder() : this(IHttpMessage.FixContentLengthHeader) { }

            //For testing
            internal Builder(Func<IEnumerable<(string, string)>, byte[], bool, IEnumerable<(string, string)>> fixContentLengthHeader)
            {
                _fixContentLengthHeader = fixContentLengthHeader;
            }


            public Builder Headers(IEnumerable<(string, string)> headers)
            {
                _headers = headers;
                return this;
            }
            public Builder Headers(params (string, string)[] headers) => Headers((IEnumerable<(string, string)>)headers);

            public Builder StatusCode(HttpStatusCode statusCode)
            {
                _statusCode = statusCode;
                return this;
            }

            public Builder Body(string body, MediaTypeHeaderValue contentType, bool createContentLengthHeader = false) => Body(Encoding.UTF8.GetBytes(body), contentType, createContentLengthHeader);

            public Builder Body(byte[] body, MediaTypeHeaderValue contentType, bool createContentLengthHeader = false)
            {
                _headers = _fixContentLengthHeader(_headers, body, createContentLengthHeader);
                _body = body;
                _contentType = contentType;
                return this;
            }

            public Builder FixContentLength()
            {
                if (_body!=null)
                {
                    _headers = _fixContentLengthHeader(_headers, _body, false);
                }
                return this;
            }

            public Builder From(IResponseMessage message)
            {
                _body = message.Body;
                _contentType = message.ContentType;
                _headers = message.Headers;
                _statusCode = message.StatusCode;
                return this;
            }

            public ServiceResponse Build() 
            {
                return new ServiceResponse(_body, _contentType, _statusCode, _headers);
            }
        }

    }
}
