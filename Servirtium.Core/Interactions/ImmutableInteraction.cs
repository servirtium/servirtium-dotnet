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
            MediaTypeHeaderValue? requestContentType,
            IEnumerable<(string, string)> requestHeaders,
            string? requestBody,
            HttpStatusCode statusCode,
            MediaTypeHeaderValue? responseContentType,
            IEnumerable<(string, string)> responseHeaders,
            string? responseBody)
        {
            Number = number;
            Notes = notes;
            Method = method;
            Path = path;

            //Request
            RequestContentType = requestContentType;
            RequestHeaders = requestHeaders;
            RequestBody = requestBody;

            //Response
            StatusCode = statusCode;
            ResponseContentType = responseContentType;
            ResponseHeaders = responseHeaders;
            ResponseBody = responseBody;
        }

        public int Number { get; }

        public IEnumerable<IInteraction.Note> Notes { get; }

        public HttpMethod Method { get; }

        public string Path { get; }

        public MediaTypeHeaderValue? RequestContentType { get; }

        public IEnumerable<(string, string)> RequestHeaders { get; }

        public string? RequestBody { get; }

        public HttpStatusCode StatusCode { get; }

        public MediaTypeHeaderValue? ResponseContentType { get; }

        public IEnumerable<(string, string)> ResponseHeaders { get; }

        public string? ResponseBody { get; }

        public class Builder
        {
            private bool _built = false;

            private int _number;

            private IEnumerable<IInteraction.Note> _notes = new IInteraction.Note[0];

            private HttpMethod _method = HttpMethod.Get;

            private string _path = "";

            private MediaTypeHeaderValue? _requestContentType;

            private IEnumerable<(string, string)> _requestHeaders = new (string, string)[0];

            private string? _requestBody;

            private HttpStatusCode _statusCode;

            private MediaTypeHeaderValue? _responseContentType;

            private IEnumerable<(string, string)> _responseHeaders = new (string, string)[0];

            private string? _responseBody;

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
                _requestBody = requestBody;
                _requestContentType = requestContentType;
                return this;
            }

            public Builder RemoveRequestBody()
            {
                _requestBody = null;
                _requestContentType = null;
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
                _responseBody = responseBody;
                _responseContentType = responseContentType;
                return this;
            }

            public Builder RemoveResponseBody()
            {
                _responseBody = null;
                _responseContentType = null;
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
                if (existing.RequestBody != null && existing.RequestContentType!=null)
                {
                    RequestBody(existing.RequestBody, existing.RequestContentType);
                }
                else 
                {
                    RemoveRequestBody();
                }
                if (existing.ResponseBody != null && existing.ResponseContentType!=null)
                {
                    ResponseBody(existing.ResponseBody, existing.ResponseContentType);
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

                    return new ImmutableInteraction(_number, _notes, _method, _path, _requestContentType, _requestHeaders, _requestBody, _statusCode, _responseContentType, _responseHeaders, _responseBody);
                }
                throw new InvalidOperationException("This builder class is only intended to build a single instance. Use a new Builder for each instance you want to create.");
            }
        }
    }
}
