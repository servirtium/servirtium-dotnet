using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Servirtium.Core
{
    public class SimpleInteractionTransforms : IInteractionTransforms
    {
        private readonly string _realServiceHost;
        private readonly HashSet<string> _requestHeadersToRemove, _responseHeadersToRemove;

        public SimpleInteractionTransforms(Uri realServiceHost, IEnumerable<string> requestHeadersToRemove, IEnumerable<string> responseHeadersToRemove)
        {
            _realServiceHost = realServiceHost.Host;
            _requestHeadersToRemove = new HashSet<string>(requestHeadersToRemove.Select(h=>h.ToLower()));
            _responseHeadersToRemove = new HashSet<string>(responseHeadersToRemove.Select(h => h.ToLower()));
        }

        public IInteraction TransformClientRequestForRealService(IInteraction clientRequest)
        {
            var builder = new MarkdownInteraction.Builder()
                .From(clientRequest)
                .RequestHeaders(clientRequest.RequestHeaders
                    //Remove unwanted headers from client request
                    .Where(h => !_requestHeadersToRemove.Contains(h.Item1.ToLower()))
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
