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
        public IEnumerable<(string Name, string Value)> Headers { get; }
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

        public ServiceResponse WithRevisedBody(string body, bool createContentLengthHeader = false)
        {
            //ToArray() required to evaluate the Select and set 'createContentLengthHeader' to false if a content length is found
            IEnumerable<(string, string)> headersWithAdjustedContentLength = Headers.Select(h =>
            {
                if (h.Name == "Content-Length" || h.Name == "content-length")
                {
                    createContentLengthHeader = false;
                    return (h.Name, (body.Length).ToString());
                }
                else return h;
            }).ToArray();
            if (createContentLengthHeader)
            {
                headersWithAdjustedContentLength = headersWithAdjustedContentLength.Append(("Content-Length", body.Length.ToString()));
            }
            return new ServiceResponse(body, ContentType, StatusCode, headersWithAdjustedContentLength);
        }
            

        public ServiceResponse WithReadjustedContentLength() => (Body is string stringBody) ? WithRevisedBody(stringBody) : this;
    }
}
