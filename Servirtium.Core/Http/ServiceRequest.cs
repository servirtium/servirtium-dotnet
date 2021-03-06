﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Servirtium.Core.Http
{
    public class ServiceRequest : IRequestMessage
    {
        public ServiceRequest(Uri url, HttpMethod method, IEnumerable<(string Name, string Value)> headers, (byte[] Content, MediaTypeHeaderValue Type)? body)
        {
            Url = url;
            Method = method;
            Headers = headers;
            Body = body;
        }

        public Uri Url { get; }

        public HttpMethod Method { get; }

        public IEnumerable<(string Name, string Value)> Headers { get; }

        public (byte[], MediaTypeHeaderValue)? Body { get; }

        public override bool Equals(object? obj)
        {
            return obj is ServiceRequest request &&
                   EqualityComparer<Uri>.Default.Equals(Url, request.Url) &&
                   EqualityComparer<HttpMethod>.Default.Equals(Method, request.Method) &&
                   Enumerable.SequenceEqual(Headers, request.Headers) &&
                   (
                       (!Body.HasValue && !request.Body.HasValue)
                       || (Body.HasValue && request.Body.HasValue &&
                            EqualityComparer<MediaTypeHeaderValue>.Default.Equals(Body.Value.Item2, request.Body.Value.Item2) &&
                            Enumerable.SequenceEqual(Body.Value.Item1, request.Body.Value.Item1)
                            )
                   );
        }

        public static bool operator ==(ServiceRequest? left, ServiceRequest? right)
        {
            return 
                (left==null && right==null) ||
                (left!=null && right!=null && EqualityComparer<ServiceRequest>.Default.Equals(left, right));
        }

        public static bool operator !=(ServiceRequest? left, ServiceRequest? right)
        {
            return !(left == right);
        }

        public class Builder
        {
            public Builder():this(IHttpMessage.FixContentLengthHeader) { }

            //For testing
            internal Builder(Func<IEnumerable<(string, string)>, byte[], bool, IEnumerable<(string, string)>> fixContentLengthHeader)
            {
                _fixContentLengthHeader = fixContentLengthHeader;
            }

            private readonly Func<IEnumerable<(string, string)>, byte[], bool, IEnumerable<(string, string)>> _fixContentLengthHeader;
            private Uri _url = new Uri("http://localhost");
            private IEnumerable<(string Name, string Value)> _headers = new (string Name, string Value)[0];
            private (byte[] Content, MediaTypeHeaderValue Type)? _body;
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
                _headers = _fixContentLengthHeader(_headers, body, createContentLengthHeader);
                _body = (body, contentType);
                return this;
            }

            public Builder FixContentLength()
            {
                if (_body != null)
                {
                    _headers = _fixContentLengthHeader(_headers, _body.Value.Content, false);
                }
                return this;
            }

            public Builder From(IRequestMessage message)
            {
                _body = message.Body;
                _headers = message.Headers;
                _method = message.Method;
                _url = message.Url;
                return this;
            }

            public ServiceRequest Build()
            {
                return new ServiceRequest(_url, _method, _headers, _body);
            }
        }
    }
}
