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
        public (byte[] Content, MediaTypeHeaderValue Type)? Body { get; }
        public HttpStatusCode StatusCode { get; }

        private ServiceResponse((byte[] Content, MediaTypeHeaderValue Type)? body, HttpStatusCode statusCode, IEnumerable<(string, string)> headers)
        {
            Headers = headers;
            Body = body;
            StatusCode = statusCode;
        }
        public override bool Equals(object? obj)
        {
            return obj is ServiceResponse request &&
                   EqualityComparer<HttpStatusCode>.Default.Equals(StatusCode, request.StatusCode) &&
                   Enumerable.SequenceEqual(Headers, request.Headers) &&
                   (
                       (!Body.HasValue && !request.Body.HasValue)
                       || (Body.HasValue && request.Body.HasValue &&
                            EqualityComparer<MediaTypeHeaderValue>.Default.Equals(Body.Value.Item2, request.Body.Value.Item2) &&
                            Enumerable.SequenceEqual(Body.Value.Item1, request.Body.Value.Item1)
                            )
                   );
        }

        public static bool operator ==(ServiceResponse? left, ServiceResponse? right)
        {
            return
                (left == null && right == null) ||
                (left != null && right != null && EqualityComparer<ServiceResponse>.Default.Equals(left, right));
        }

        public static bool operator !=(ServiceResponse? left, ServiceResponse? right)
        {
            return !(left == right);
        }

        public class Builder
        {
            private IEnumerable<(string Name, string Value)> _headers = new (string Name, string Value)[0];
            private (byte[] Content, MediaTypeHeaderValue Type)? _body;
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
                _body = (body, contentType);
                return this;
            }

            public Builder FixContentLength()
            {
                if (_body!=null)
                {
                    _headers = _fixContentLengthHeader(_headers, _body.Value.Content, false);
                }
                return this;
            }

            public Builder From(IResponseMessage message)
            {
                _body = message.Body;
                _headers = message.Headers;
                _statusCode = message.StatusCode;
                return this;
            }

            public ServiceResponse Build() 
            {
                return new ServiceResponse(_body, _statusCode, _headers);
            }
        }

    }
}
