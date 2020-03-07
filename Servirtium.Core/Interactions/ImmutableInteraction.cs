using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace Servirtium.Core.Interactions
{
    public class ImmutableInteraction : IInteraction
    {
        
        //private constructor for use in builder
        private ImmutableInteraction(
            int number,
            IEnumerable<IInteraction.Note> notes,
            HttpMethod method,
            string path,
            IEnumerable<(string, string)> requestHeaders,
            (string Content, MediaTypeHeaderValue Type)? requestBody,
            HttpStatusCode statusCode,
            IEnumerable<(string, string)> responseHeaders,
            (string Content, MediaTypeHeaderValue Type)? responseBody)
        {
            Number = number;
            Notes = notes;
            Method = method;
            Path = path;

            //Request
            RequestHeaders = requestHeaders;
            RequestBody = requestBody;

            //Response
            StatusCode = statusCode;
            ResponseHeaders = responseHeaders;
            ResponseBody = responseBody;
        }

        public int Number { get; }

        public IEnumerable<IInteraction.Note> Notes { get; }

        public HttpMethod Method { get; }

        public string Path { get; }

        public IEnumerable<(string, string)> RequestHeaders { get; }

        public (string Content, MediaTypeHeaderValue Type)? RequestBody { get; }

        public HttpStatusCode StatusCode { get; }

        public IEnumerable<(string, string)> ResponseHeaders { get; }

        public (string Content, MediaTypeHeaderValue Type)? ResponseBody { get; }

        public class Builder
        {
            private bool _built = false;

            private int _number;

            private IEnumerable<IInteraction.Note> _notes = new IInteraction.Note[0];

            private HttpMethod _method = HttpMethod.Get;

            private string _path = "";

            private IEnumerable<(string, string)> _requestHeaders = new (string, string)[0];

            private (string Content, MediaTypeHeaderValue Type)? _requestBody;

            private HttpStatusCode _statusCode;

            private IEnumerable<(string, string)> _responseHeaders = new (string, string)[0];

            private (string Content, MediaTypeHeaderValue Type)? _responseBody;

            public Builder Number(int number)
            {
                _number = number;
                return this;
            }

            public Builder Notes(IEnumerable<IInteraction.Note> notes)
            {
                _notes = notes;
                return this;
            }

            public Builder Method(HttpMethod method)
            {
                _method = method;
                return this;
            }

            public Builder Path(string path)
            {
                _path = path;
                return this;
            }

            public Builder RequestHeaders(IEnumerable<(string, string)> requestHeaders)
            {
                _requestHeaders = requestHeaders;
                return this;
            }

            public Builder RequestBody(string requestBody, MediaTypeHeaderValue requestContentType)
            {
                _requestBody = (requestBody, requestContentType);
                return this;
            }

            public Builder RemoveRequestBody()
            {
                _requestBody = null;
                return this;
            }

            public Builder StatusCode(HttpStatusCode statusCode)
            {
                _statusCode = statusCode;
                return this;
            }

            public Builder ResponseHeaders(IEnumerable<(string, string)> responseHeaders)
            {
                _responseHeaders = responseHeaders;
                return this;
            }

            public Builder ResponseBody(string responseBody, MediaTypeHeaderValue responseContentType)
            {
                _responseBody = (responseBody, responseContentType);
                return this;
            }

            public Builder RemoveResponseBody()
            {
                _responseBody = null;
                return this;
            }

            public Builder From(IInteraction existing)
            {
                Number(existing.Number);
                Notes(existing.Notes);
                Method(existing.Method);
                Path(existing.Path);
                RequestHeaders(existing.RequestHeaders);
                ResponseHeaders(existing.ResponseHeaders);
                StatusCode(existing.StatusCode);
                if (existing.RequestBody.HasValue)
                {
                    var (content, type) = existing.RequestBody.Value;
                    RequestBody(content, type);
                }
                else
                {
                    RemoveRequestBody();
                }

                if (existing.ResponseBody.HasValue)
                {
                    var (content, type) = existing.ResponseBody.Value;
                    ResponseBody(content, type);
                }
                else
                {
                    RemoveResponseBody();
                }
                return this;
            }

            public ImmutableInteraction Build()
            {
                if (!_built)
                {
                    _built = true;

                    return new ImmutableInteraction(_number, _notes, _method, _path, _requestHeaders, _requestBody, _statusCode, _responseHeaders, _responseBody);
                }
                throw new InvalidOperationException("This builder class is only intended to build a single instance. Use a new Builder for each instance you want to create.");
            }
        }
    }
}
