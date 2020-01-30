using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Servirtium.Core
{
    public class SimpleInteractionTransforms : IInteractionTransforms
    {


        private readonly string _realServiceHost;
        private readonly IEnumerable<Regex> _requestHeaderExcludePatterns, _responseHeadersExcludePatterns;


        public SimpleInteractionTransforms(Uri realServiceHost) : this(realServiceHost, new Regex[0], new Regex[0])
        { }

        public SimpleInteractionTransforms(Uri realServiceHost, IEnumerable<Regex> requestHeaderExcludePatterns, IEnumerable<Regex> responseHeadersExcludePatterns)
        {
            _realServiceHost = realServiceHost.Host;
            _requestHeaderExcludePatterns = requestHeaderExcludePatterns;
            _responseHeadersExcludePatterns = responseHeadersExcludePatterns;
        }

        public IInteraction TransformClientRequestForRealService(IInteraction clientRequest)
        {
            var builder = new MarkdownInteraction.Builder()
                .From(clientRequest)
                .RequestHeaders(clientRequest.RequestHeaders
                    //Remove unwanted headers from client request
                    .Where(h => !_requestHeaderExcludePatterns.Any(pattern=>pattern.IsMatch($"{h.Item1}: {h.Item2}")))
                    //Fix host header
                    .Select(h =>
                    {
                        (string name, string value) = h;
                        if (name.ToLower() == "host")
                        {
                            return (name, _realServiceHost);
                        }
                        else return h;
                    })
                );
            return builder.Build();
        }
    }
}
