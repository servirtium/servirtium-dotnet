using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace Servirtium.Core
{
    public class MarkdownInteraction : IInteraction
    {
        //Parses markdown for a single interaction and captures the content into named capture groups
        private static readonly Regex INTERACTION_REGEX = new Regex(
            @"(?xm)
              \#\#\s+Interaction\s+(?<number>[0-9]+):\s+(?<method>[A-Za-z]+)\s+(?<path>[^\n\r]+)                                        # Interaction title                 ## Interaction 0: POST /my-api/rest/v1/stuff
              [\r\n]+                                                                                                                   # (captures 'method' and 'path')
              \#\#\#\s+Request\s+headers\s+recorded\s+for\s+playback:                                                                   # Request Header Title              ### Request headers recorded for playback: 
              [\r\n]+                                                                                                                   #
              ```(?:\n|\r|\r\n)(?<requestHeaders>[\w\W]*?)(?:\n|\r|\r\n)```                                                             # Request Headers                   ```
                                                                                                                                        # (captures 'requestHeaders')       Accept: text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2
                                                                                                                                        #                                   User-Agent: Servirtium-Testing
                                                                                                                                        #                                   Connection: keep-alive
                                                                                                                                        #                                   Host: example.com
                                                                                                                                        #                                   ```
              [\r\n]+                                                                                                                   #
              \#\#\#\s+Request\s+body\s+recorded\s+for\s+playback\s+\((?<requestContentType>[^\n\r]+)?\):                               # Request Body Title                ### Request body recorded for playback (text/plain):
              [\r\n]+                                                                                                                   # (captures 'requestContentType')
              ```(?:\n|\r|\r\n)(?<requestBody>[\w\W]*?)(?:\n|\r|\r\n)```                                                                # Request Body                      ```
                                                                                                                                        # (captures 'requestBody')          request body contents
                                                                                                                                        #                                   ```
              [\r\n]+
              \#\#\#\s+Response\s+headers\s+recorded\s+for\s+playback:                                                                    #Response Header Title              ### Response headers recorded for playback:
              [\r\n]+
              ```(?:\n|\r|\r\n)(?<responseHeaders>[\w\W]*?)(?:\n|\r|\r\n)```                                                            # Response Headers                  ```
                                                                                                                                        # (captures 'responseHeaders')      Content-Type: application/json
                                                                                                                                        #                                   Connection: keep-alive
                                                                                                                                        #                                   Transfer-Encoding: chunked
                                                                                                                                        #                                   ```
              [\r\n]+
              \#\#\#\s+Response\s+body\s+recorded\s+for\s+playback\s+\((?<statusCode>[0-9]+):\s+(?<responseContentType>[^\n\r]+)?\):    # Response Body Title               ### Response body recorded for playback (200: application/json):
              [\r\n]+                                                                                                                   # (captures 'statusCode' 
                                                                                                                                        # and 'responseContentType')
              ```(?:\n|\r|\r\n)(?<responseBody>[\w\W]*?)(?:\n|\r|\r\n)```                                                               # Response Body                     ```
                                                                                                                                        # (captures 'responseBody')         response body contents
                                                                                                                                        #                                   ```
            "
        , RegexOptions.Singleline);

        private static IEnumerable<(string, string)> HeaderTextToHeaderList(string headerText)
           => headerText.Split('\n')
                //remove empty lines first
                .Where(line => !String.IsNullOrWhiteSpace(line))
                .Select(line => {
                    var bits = line.Split(":");
                    if (bits.Length < 2)
                    {
                        throw new ArgumentException($"This headers contain a line that was not formatted as a valid HTTP header (i.e. <NAME>: <VALUE>): {headerText}", nameof(headerText));
                    }
                    return (bits[0].Trim(), String.Join(":", bits.Skip(1)).Trim());
                });

        //private constructor for use in builder
        private MarkdownInteraction(
            int number,
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

            private HttpMethod _method = HttpMethod.Get;

            private string _path = "";

            private MediaTypeHeaderValue? _requestContentType;

            private IEnumerable<(string, string)> _requestHeaders = new (string, string)[0];

            private string? _requestBody;

            private HttpStatusCode _statusCode;

            private MediaTypeHeaderValue? _responseContentType;

            private IEnumerable<(string, string)> _responseHeaders = new (string, string)[0];

            private string? _responseBody;

            //Sets the builder up from an input markdown script. This will be used as a baseline and invididual property settings will be applied later
            public Builder Markdown(string markdown)
            {

                var match = INTERACTION_REGEX.Match(markdown);
                if (!match.Success)
                {
                    throw new ArgumentException($"This markdown could not be parsed as an interaction: {markdown}");
                }
                _number = Int32.Parse(match.Groups["number"].Value);
                _method = new HttpMethod(match.Groups["method"].Value);
                _path = match.Groups["path"].Value;

                //Request
                _requestContentType = match.Groups["requestContentType"].Success ? new MediaTypeHeaderValue(match.Groups["requestContentType"].Value) : null;
                _requestHeaders = HeaderTextToHeaderList(match.Groups["requestHeaders"].Value);
                var requestBody = match.Groups["requestBody"].Value;
                _requestBody = requestBody.Any() ? requestBody : null;

                //Response
                _statusCode = Enum.Parse<HttpStatusCode>(match.Groups["statusCode"].Value);
                _responseContentType = match.Groups["responseContentType"].Success ? new MediaTypeHeaderValue(match.Groups["responseContentType"].Value) : null;
                _responseHeaders = HeaderTextToHeaderList(match.Groups["responseHeaders"].Value);
                var responseBody = match.Groups["responseBody"].Value;
                _responseBody = responseBody.Any() ? responseBody : null;

                return this;
            }

            public Builder Number(int number)
            {
                _number = number;
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
                _responseContentType = null;
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

            public MarkdownInteraction Build()
            {
                if (!_built)
                {
                    _built = true;

                    return new MarkdownInteraction(_number, _method, _path, _requestContentType, _requestHeaders, _requestBody, _statusCode, _responseContentType, _responseHeaders, _responseBody);
                }
                throw new InvalidOperationException("This builder class is only intended to build a single instance. Use a new Builder for each instance you want to create.");
            }
        }
    }
}
