using Servirtium.Core.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Servirtium.Core.Interactions
{
    public class InteractionReplayer : IInteractionMonitor
    {
        private string _filename = "no filename set";
        private IDictionary<int, IInteraction> _allInteractions;
        private readonly IScriptReader _scriptReader;

        private readonly IBodyFormatter _requestBodyFormatter;
        private readonly IBodyFormatter _responseBodyFormatter;


        public InteractionReplayer(IScriptReader? scriptReader =null, IDictionary<int, IInteraction>? interactions = null, IBodyFormatter? responseBodyFormatter = null, IBodyFormatter? requestBodyFormatter = null)
        {
            _scriptReader = scriptReader ?? new MarkdownScriptReader();
            _allInteractions = interactions ?? new Dictionary<int, IInteraction> { };
            _requestBodyFormatter = requestBodyFormatter ?? new TextAndBinaryBodyFormatter(new UTF8TextBodyFormatter(), new Base64BinaryBodyFormatter());
            _responseBodyFormatter = responseBodyFormatter ?? new TextAndBinaryBodyFormatter(new UTF8TextBodyFormatter(), new Base64BinaryBodyFormatter());

        }

        public Task<IResponseMessage> GetServiceResponseForRequest(int interactionNumber, IRequestMessage request, bool lowerCaseHeaders = false)
        {
            //Validate the request is the same as it was when it was recorded
            var recordedInteraction = _allInteractions[interactionNumber];
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
                throw new ArgumentException($"HTTP request {(request.Body.HasValue ? "does" : "does not")} have a body, whereas recorded interaction {(recordedInteraction.RequestBody.HasValue ? "does" : "does not")} for interaction {interactionNumber}.");
            }

            if (request.Body.HasValue && recordedInteraction.RequestBody.HasValue)
            {
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

            return Task.FromResult<IResponseMessage>(builder.Build());
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
            _allInteractions = _scriptReader.Read(conversationReader);
        }
    }
}
