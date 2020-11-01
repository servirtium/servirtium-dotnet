using Servirtium.Core.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace Servirtium.Core.Interactions
{
    public class InteractionReplayer : IInteractionMonitor
    {
        private readonly ILogger<InteractionReplayer> _logger;
        private string _filename = "no filename set";
        private IDictionary<int, IInteraction> _allInteractions;
        private readonly IScriptReader _scriptReader;

        private readonly IBodyFormatter _requestBodyFormatter;
        private readonly IBodyFormatter _responseBodyFormatter;

        private void LogBodies(IRequestMessage incoming, IInteraction recorded)
        {
            if (incoming.Body.HasValue)
            {
                var (content, type) = incoming.Body.Value;
                var contentString = Encoding.UTF8.GetString(content);
                _logger.LogDebug($"Incoming Request Content-Type: {type}");
                _logger.LogDebug($"Incoming Request Content:{Environment.NewLine}{contentString}{Environment.NewLine}({contentString.Length} characters)");
            }
            if (recorded.RequestBody.HasValue)
            {
                var (content, type) = recorded.RequestBody.Value;
                _logger.LogDebug($"Recorded Request Content-Type: {type}");
                _logger.LogDebug($"Recorded Request Content:{Environment.NewLine}{content}");
            }
        }

        public InteractionReplayer(IScriptReader? scriptReader =null, IDictionary<int, IInteraction>? interactions = null, IBodyFormatter? responseBodyFormatter = null, IBodyFormatter? requestBodyFormatter = null, ILoggerFactory? loggerFactory = null)
        {
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<InteractionReplayer>();
            _scriptReader = scriptReader ?? new MarkdownScriptReader(loggerFactory);
            _allInteractions = interactions ?? new Dictionary<int, IInteraction> { };
            _requestBodyFormatter = requestBodyFormatter ?? new TextAndBinaryBodyFormatter(new UTF8TextBodyFormatter(), new Base64BinaryBodyFormatter(), loggerFactory);
            _responseBodyFormatter = responseBodyFormatter ?? new TextAndBinaryBodyFormatter(new UTF8TextBodyFormatter(), new Base64BinaryBodyFormatter(), loggerFactory);

        }

        public Task<IResponseMessage> GetServiceResponseForRequest(int interactionNumber, IRequestMessage request, bool lowerCaseHeaders = false)
        {
            //Validate the request is the same as it was when it was recorded
            var recordedInteraction = _allInteractions[interactionNumber];
            _logger.LogDebug($"Validating {request.Method} to {request.Url} against interaction {interactionNumber} for replaying.");
            if (request.Url.PathAndQuery != recordedInteraction.Path)
            {
                throw new ArgumentException($"HTTP request path '{request.Url.PathAndQuery}' does not match method recorded in conversation for interaction {interactionNumber}, '{recordedInteraction.Path}'.");
            }
            if (request.Method!=recordedInteraction.Method)
            {
                throw new ArgumentException($"HTTP request method '{request.Method}' does not match method recorded in conversation for interaction {interactionNumber}, '{recordedInteraction.Method}'.");
            }

            if (!request.Headers.All(header=> recordedInteraction.RequestHeaders.Contains(header)))
            {
                throw new ArgumentException($"Fixed & filtered HTTP request headers: {Environment.NewLine}{String.Join(Environment.NewLine, request.Headers.Select(h => $"{h.Name}: {h.Value}"))}{Environment.NewLine} do not contain all the headers recorded in conversation for interaction {interactionNumber}: {Environment.NewLine}{String.Join(Environment.NewLine, recordedInteraction.RequestHeaders.Select(h=>$"{h.Name}: {h.Value}"))}.");
            }
            if (request.Body.HasValue != recordedInteraction.RequestBody.HasValue)
            {
                LogBodies(request, recordedInteraction);
                throw new ArgumentException($"HTTP request {(request.Body.HasValue ? "does" : "does not")} have a body, whereas recorded interaction {(recordedInteraction.RequestBody.HasValue ? "does" : "does not")} for interaction {interactionNumber}.");
            }

            if (request.Body.HasValue && recordedInteraction.RequestBody.HasValue)
            {
                LogBodies(request, recordedInteraction);
                var (content, type) = request.Body.Value;
                var (recordedBody, recordedType) = recordedInteraction.RequestBody.Value;
                if (!type.Equals(recordedType))
                {
                    throw new ArgumentException($"HTTP request content type '{type}' does not match method recorded in conversation for interaction {interactionNumber}, '{recordedType}'.");
                }
                var bodyString =  _requestBodyFormatter.Write(content, type);
                if (bodyString != recordedBody)
                {
                    throw new ArgumentException($"HTTP request body '{bodyString}' does not match method body in conversation for interaction {interactionNumber}, '{recordedInteraction.RequestBody}'.");
                }
            }
            
            _logger.LogDebug($"{request.Method} to {request.Url} is valid to replay against interaction {interactionNumber}.");
            
            //Return completed task, no async logic required in the playback method
            var builder = new ServiceResponse.Builder()
                .StatusCode(recordedInteraction.StatusCode)
                .Headers(recordedInteraction.ResponseHeaders);

            if (recordedInteraction.ResponseBody.HasValue)
            {
                var (content, type) = recordedInteraction.ResponseBody.Value;
                var body = _responseBodyFormatter.Read(content, type);
                builder.Body(body, type);
            }

            var response = builder.Build();
            _logger.LogDebug($"Replaying {response.StatusCode} response against {request.Method} to {request.Url} for interaction {interactionNumber}.");
            return Task.FromResult<IResponseMessage>(response);
        }


        public void LoadScriptFile(string filename) 
        {
            using (var fileContents = File.OpenText(filename))
            {
                ReadPlaybackConversation(fileContents, filename);
            }

        }

        public void ReadPlaybackConversation(TextReader conversationReader, string filename = "no filename set")
        {
            _filename = filename;
            _logger.LogDebug($"Loading interactions from '{filename}'");
            _allInteractions = _scriptReader.Read(conversationReader);
            _logger.LogInformation($"Loaded {_allInteractions.Count()} interactions from '{filename}'");
        }
    }
}
