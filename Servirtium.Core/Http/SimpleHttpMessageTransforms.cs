using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Servirtium.Core.Http
{
    public class SimpleHttpMessageTransforms : IHttpMessageTransforms
    {
        private readonly Uri? _realServiceHost;
        private readonly IEnumerable<Regex> _requestHeaderExcludePatterns, _responseHeaderExcludePatterns;

        private readonly ILogger<SimpleHttpMessageTransforms> _logger;


        public SimpleHttpMessageTransforms(Uri realServiceHost, ILoggerFactory? loggerFactory = null) : this(realServiceHost, new Regex[0], new Regex[0], loggerFactory)
        { }
        
        public SimpleHttpMessageTransforms(IEnumerable<Regex> requestHeaderExcludePatterns, IEnumerable<Regex> responseHeadersExcludePatterns, ILoggerFactory? loggerFactory = null) : this(null, requestHeaderExcludePatterns, responseHeadersExcludePatterns, loggerFactory)
        { }

        public SimpleHttpMessageTransforms(Uri? realServiceHost, IEnumerable<Regex> requestHeaderExcludePatterns, IEnumerable<Regex> responseHeadersExcludePatterns, ILoggerFactory? loggerFactory = null)
        {
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SimpleHttpMessageTransforms>();
            _realServiceHost = realServiceHost !=null ?
                new Uri(realServiceHost.GetLeftPart(UriPartial.Authority)) : null;
            _requestHeaderExcludePatterns = requestHeaderExcludePatterns;
            _responseHeaderExcludePatterns = responseHeadersExcludePatterns;
        }

        private static string ExtractHostAndPort(Uri fullUri)
        {
            var schemeAndDelimiterLength = fullUri.Scheme.Length + Uri.SchemeDelimiter.Length;
            //reverse proxy mode, swap out Host header for configured real service
            return fullUri.GetLeftPart(UriPartial.Authority)[schemeAndDelimiterLength..];
        }

        public IRequestMessage TransformClientRequestForRealService(IRequestMessage clientRequest)
        {
            _logger.LogDebug($"Transforming {clientRequest.Method} request to {clientRequest.Url} for forwarding to service");
            string hostAndPort;
            if (_realServiceHost != null)
            {
                //reverse proxy mode, swap out Host header for real service set at startup
                hostAndPort = ExtractHostAndPort(_realServiceHost);
            }
            else
            {
                //forward proxy mode, swap out Host header for url in request
                hostAndPort = ExtractHostAndPort(clientRequest.Url);
            }
            
            var builder = new ServiceRequest.Builder()
                .From(clientRequest)
                .Headers(clientRequest.Headers
                    //Remove unwanted headers from client request
                    .Where(h => !_requestHeaderExcludePatterns.Any(pattern=>pattern.IsMatch($"{h.Item1}: {h.Item2}")))
                    //Fix host header
                    .Select(h =>
                    {
                        (string name, string value) = h;
                        if (name.ToLower() == "host")
                        {
                            return (name, hostAndPort);
                        }
                        else return h;
                    })
                );
            if (_realServiceHost != null)
            {
                builder.Url(new Uri(_realServiceHost, clientRequest.Url.PathAndQuery));
            }
            var transformed = builder.Build();
            _logger.LogDebug($"Transformed {transformed.Method} request to {transformed.Url} for forwarding to service");
            return transformed;
        }


        public IResponseMessage TransformRealServiceResponseForClient(IResponseMessage serviceResponse)=> new ServiceResponse.Builder()
            .From(serviceResponse)
            .Headers
            (    //Remove unwanted headers from service response
                serviceResponse.Headers.Where(h => !_responseHeaderExcludePatterns.Any(pattern => pattern.IsMatch($"{h.Item1}: {h.Item2}")))
            )
            .Build();
    }
}
