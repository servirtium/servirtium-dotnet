using Servirtium.Core.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using System.Threading;

namespace Servirtium.Core.Interactions
{
    public class InteractionReplayer : IInteractionMonitor
    {
        private readonly ILogger<InteractionReplayer> _logger;
        private IDictionary<int, IInteraction> _allInteractions;
        private ICollection<(IRequestMessage Request, TaskCompletionSource<IInteraction> TaskCompletionSource)> _pendingRequests = new HashSet<(IRequestMessage, TaskCompletionSource<IInteraction>)>();
        private ICollection<IInteraction> _pendingInteractions = new HashSet<IInteraction>();
        private readonly IScriptReader _scriptReader;

        private readonly IBodyFormatter _requestBodyFormatter;
        private readonly IBodyFormatter _responseBodyFormatter;

        private readonly TimeSpan _concurrentRequestWindow;

        public InteractionReplayer(IScriptReader? scriptReader, TimeSpan concurrentRequestWindow, IDictionary<int, IInteraction>? interactions = null, IBodyFormatter? responseBodyFormatter = null, IBodyFormatter? requestBodyFormatter = null, ILoggerFactory? loggerFactory = null)
        {
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<InteractionReplayer>();
            _scriptReader = scriptReader ?? new MarkdownScriptReader(loggerFactory);
            _allInteractions = interactions ?? new Dictionary<int, IInteraction> { };
            _requestBodyFormatter = requestBodyFormatter ?? new TextAndBinaryBodyFormatter(new UTF8TextBodyFormatter(), new Base64BinaryBodyFormatter(), loggerFactory);
            _responseBodyFormatter = responseBodyFormatter ?? new TextAndBinaryBodyFormatter(new UTF8TextBodyFormatter(), new Base64BinaryBodyFormatter(), loggerFactory);
            _concurrentRequestWindow = concurrentRequestWindow;
        }

        public InteractionReplayer(IScriptReader? scriptReader =null, IDictionary<int, IInteraction>? interactions = null, IBodyFormatter? responseBodyFormatter = null, IBodyFormatter? requestBodyFormatter = null, ILoggerFactory? loggerFactory = null)
            :this(scriptReader, TimeSpan.FromSeconds(1), interactions, responseBodyFormatter, requestBodyFormatter, loggerFactory)
        { }

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

        private (bool IsValid, string Message) ValidateRequest(IRequestMessage request, IInteraction recordedInteraction)
        {
            int interactionNumber = recordedInteraction.Number;
            _logger.LogDebug($"Validating {request.Method} to {request.Url} against interaction {interactionNumber} for replaying.");
            if (request.Url.PathAndQuery != recordedInteraction.Path)
            {
                return (false, $"HTTP request path '{request.Url.PathAndQuery}' does not match method recorded in conversation for interaction {interactionNumber}, '{recordedInteraction.Path}'.");
            }
            if (request.Method != recordedInteraction.Method)
            {
                return (false, $"HTTP request method '{request.Method}' does not match method recorded in conversation for interaction {interactionNumber}, '{recordedInteraction.Method}'.");
            }

            if (!request.Headers.All(header => recordedInteraction.RequestHeaders.Contains(header)))
            {
                return (false, $"Fixed & filtered HTTP request headers: {Environment.NewLine}{String.Join(Environment.NewLine, request.Headers.Select(h => $"{h.Name}: {h.Value}"))}{Environment.NewLine} do not contain all the headers recorded in conversation for interaction {interactionNumber}: {Environment.NewLine}{String.Join(Environment.NewLine, recordedInteraction.RequestHeaders.Select(h => $"{h.Name}: {h.Value}"))}.");
            }
            if (request.Body.HasValue != recordedInteraction.RequestBody.HasValue)
            {
                LogBodies(request, recordedInteraction);
                return (false, $"HTTP request {(request.Body.HasValue ? "does" : "does not")} have a body, whereas recorded interaction {(recordedInteraction.RequestBody.HasValue ? "does" : "does not")} for interaction {interactionNumber}.");
            }

            if (request.Body.HasValue && recordedInteraction.RequestBody.HasValue)
            {
                LogBodies(request, recordedInteraction);
                var (content, type) = request.Body.Value;
                var (recordedBody, recordedType) = recordedInteraction.RequestBody.Value;
                if (!type.Equals(recordedType))
                {
                    return (false, $"HTTP request content type '{type}' does not match method recorded in conversation for interaction {interactionNumber}, '{recordedType}'.");
                }
                var bodyString = _requestBodyFormatter.Write(content, type);
                if (bodyString != recordedBody)
                {
                    return (false, $"HTTP request body '{bodyString}' does not match method body in conversation for interaction {interactionNumber}, '{recordedInteraction.RequestBody}'.");
                }
            }
            return (true, $"{request.Method} to {request.Url} is valid to replay against interaction {interactionNumber}.");
        }

        public async Task<IResponseMessage> GetServiceResponseForRequest(int interactionNumber, IRequestMessage request, bool lowerCaseHeaders = false)
        {
            //Validate the request is the same as it was when it was recorded
            var nextInteraction = _allInteractions[interactionNumber];
            IInteraction? matchedInteraction = null;
            var tcs = new TaskCompletionSource<IInteraction>();
            var requestAndTcs = (request, tcs);
            var finalError = new StringBuilder();

            lock (_pendingInteractions)
            {
                //Add the new interaction to a pending list then check if the request matches any current pending interactions
                _pendingInteractions.Add(nextInteraction);
                foreach (var pendingInteraction in _pendingInteractions)
                {
                    var validationResult = ValidateRequest(request, pendingInteraction);
                    if (validationResult.IsValid)
                    {
                        _logger.LogDebug($"Pending interaction matched against request - {validationResult.Message}");
                        matchedInteraction = pendingInteraction;
                        break;
                    }
                    else
                    {
                        var err = $"Pending interaction not matched against request - {validationResult.Message}";
                        _logger.LogWarning(err);
                        finalError.AppendLine(err);
                    }
                }

                if (nextInteraction != matchedInteraction)
                {
                    //If the request is matched against an interaction other than the new one supplied (or not matched at all), 
                    //we need to see if the new interaction can be matched any outstanding requests
                    foreach (var pendingRequest in _pendingRequests)
                    {
                        var validationResult = ValidateRequest(pendingRequest.Request, nextInteraction);
                        if (validationResult.IsValid)
                        {
                            pendingRequest.TaskCompletionSource.SetResult(nextInteraction);
                            _logger.LogDebug($"Pending request matched against interaction - {validationResult.Message}");
                            break;
                        }
                        else
                        {
                            _logger.LogWarning($"Pending request not matched against interaction - {validationResult.Message}");
                        }
                    }
                }
                //If the request has yet to be matched against an interaction, we wait here on a task that only completes if a matching interaction is found against a subsequent request
                //If a matching interaction doesn't come in within the specified timeout window, an exception is thrown and the test run fails
                if (matchedInteraction == null)
                {
                    _logger.LogDebug($"Request '{request.Method} {request.Url}' not matched against pending interactions, adding it to pending list to check against future interactions");
                    requestAndTcs = (request, tcs);
                    _pendingRequests.Add(requestAndTcs);
                }
            }
            if (matchedInteraction == null)
            {
                try
                {
                    using (var cts = new CancellationTokenSource(_concurrentRequestWindow))
                    {
                        cts.Token.Register(() => tcs.TrySetCanceled()); 
                        _logger.LogDebug($"Waiting for request '{request.Method} {request.Url}' to be matched");
                        matchedInteraction = await requestAndTcs.tcs.Task;
                        _logger.LogDebug($"Request '{request.Method} {request.Url}' matched to interaction {matchedInteraction.Number}, resuming serviceing request.");
                    }
                }
                catch (TaskCanceledException ex)
                {                   
                    _logger.LogError($"Interaction was not matched against a request within the {_concurrentRequestWindow.TotalSeconds} second timeout.", ex);
                    throw new PlaybackException($"Interaction was not matched against a request within the {_concurrentRequestWindow.TotalSeconds} second timeout.{Environment.NewLine}{finalError}", ex);
                }
                _pendingRequests.Remove(requestAndTcs);
            }

            _pendingInteractions.Remove(matchedInteraction);

            //Return completed task, no async logic required in the playback method
            var builder = new ServiceResponse.Builder()
                .StatusCode(matchedInteraction.StatusCode)
                .Headers(matchedInteraction.ResponseHeaders);

            if (matchedInteraction.ResponseBody.HasValue)
            {
                var (content, type) = matchedInteraction.ResponseBody.Value;
                var body = _responseBodyFormatter.Read(content, type);
                builder.Body(body, type);
            }

            var response = builder.Build();
            _logger.LogDebug($"Replaying {response.StatusCode} response against {request.Method} to {request.Url} for interaction {matchedInteraction.Number}.");
            return response;
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
            _logger.LogDebug($"Loading interactions from '{filename}'");
            try
            {
                _allInteractions = _scriptReader.Read(conversationReader);
            }
            catch (ArgumentException e)
            {
                if (filename.Equals("no filename set"))
                {
                    throw e;
                }
                var fullPath = Path.GetFullPath(filename);
                throw new ArgumentException("Markdown file that may be missing: " + fullPath, e);
            }
            _logger.LogInformation($"Loaded {_allInteractions.Count()} interactions from '{filename}'");
        }
    }
}
