using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Servirtium.Core.Interactions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Servirtium.Core.Http
{
    public class FindAndReplaceHttpMessageTransforms : IHttpMessageTransforms
    {
        private readonly ILogger<FindAndReplaceHttpMessageTransforms> _logger;

        private readonly IEnumerable<RegexReplacement> _replacementsForRecording;


        public FindAndReplaceHttpMessageTransforms(IEnumerable<RegexReplacement> replacementsForRecording, ILoggerFactory? loggerFactory = null)
        {
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<FindAndReplaceHttpMessageTransforms>();
            _replacementsForRecording = replacementsForRecording;
        }

        public IRequestMessage TransformClientRequestForRealService(IRequestMessage clientRequest)
        {
            var builder = new ServiceRequest.Builder()
                .From(clientRequest)
                .Headers(clientRequest.Headers.Select(h => RegexReplacement.FixHeaderForRecording(h, _replacementsForRecording, ReplacementContext.RequestHeader)));

            if (clientRequest.Body.HasValue && TextAndBinaryBodyFormatter.IsTextMediaType(clientRequest.Body.Value.Type))
            {
                builder = builder.Body(
                    RegexReplacement.FixStringForRecording(Encoding.UTF8.GetString(clientRequest.Body.Value.Content), _replacementsForRecording, ReplacementContext.RequestBody),
                    clientRequest.Body.Value.Type
                );
            }
            
            return builder
                .Build();
        }

        public IResponseMessage TransformRealServiceResponseForClient(IResponseMessage serviceResponse)
        {
            var builder = new ServiceResponse.Builder()
                .From(serviceResponse)
                .Headers(serviceResponse.Headers.Select(h => RegexReplacement.FixHeaderForRecording(h, _replacementsForRecording, ReplacementContext.ResponseHeader)));

            if (serviceResponse.Body.HasValue && TextAndBinaryBodyFormatter.IsTextMediaType(serviceResponse.Body.Value.Type))
            {
                builder = builder.Body(
                    RegexReplacement.FixStringForRecording(Encoding.UTF8.GetString(serviceResponse.Body.Value.Content), _replacementsForRecording, ReplacementContext.ResponseBody), 
                    serviceResponse.Body.Value.Type
                );
            }

            return builder.Build();
        }
    }
}
