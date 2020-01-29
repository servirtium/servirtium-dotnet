using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Servirtium.Core
{
    public class ServiceResponse
    {
        public IEnumerable<(string, string)> Headers { get; }
        public object? Body { get; }
        public MediaTypeHeaderValue? ContentType { get; }
        public HttpStatusCode StatusCode { get; }

        public ServiceResponse(object? body, MediaTypeHeaderValue? contentType, HttpStatusCode statusCode, IEnumerable<(string, string)> headers)
        {
            Headers = headers;
            Body = body;
            ContentType = contentType;
            StatusCode = statusCode;
        }
        public ServiceResponse(object? body, MediaTypeHeaderValue? contentType, HttpStatusCode statusCode, params (string, string)[] headers) : this(body, contentType, statusCode, (IEnumerable<(string, string)>)headers) { }

        public ServiceResponse WithRevisedHeaders(IEnumerable<(string, string)> headers)=>new ServiceResponse(Body, ContentType, StatusCode, headers);

        public ServiceResponse WithRevisedBody(string body) => 
            new ServiceResponse(body, ContentType, StatusCode, Headers.Select(h =>
            {
                (string name, string value) = h;
                if (name == "Content-Length" || name == "content-length")
                {
                    return (name, (body?.Length ?? 0).ToString());
                }
                else return h;
            }).ToArray());
    }
}
