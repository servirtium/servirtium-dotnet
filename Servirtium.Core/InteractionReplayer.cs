using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Servirtium.Core
{
    public class InteractionReplayer : IInteractionMonitor
    {
        private string _filename = "no filename set";
        private IDictionary<int, IInteraction> _allInteractions;
        private readonly IScriptReader _scriptReader;

        public InteractionReplayer(IScriptReader? scriptReader =null, IDictionary<int, IInteraction>? interactions = null)
        {
            _scriptReader = scriptReader ?? new MarkdownScriptReader();
            _allInteractions = interactions ?? new Dictionary<int, IInteraction> { };
        }

        public Task<ServiceResponse> GetServiceResponseForRequest(Uri host, IInteraction interaction, bool lowerCaseHeaders = false)
        {
            //Validate the request is the same as it was when it was recorded
            var recordedInteraction = _allInteractions[interaction.Number];
            if (interaction.Path != recordedInteraction.Path)
            {
                throw new ArgumentException($"HTTP request path '{interaction.Path}' does not match method recorded in conversation for interaction {interaction.Number}, '{recordedInteraction.Path}'.");
            }
            if (interaction.Method!=recordedInteraction.Method)
            {
                throw new ArgumentException($"HTTP request method '{interaction.Method}' does not match method recorded in conversation for interaction {interaction.Number}, '{recordedInteraction.Method}'.");
            }

            if (!interaction.RequestHeaders.All(header=> recordedInteraction.RequestHeaders.Contains(header)))
            {
                throw new ArgumentException($"Fixed & filtered HTTP request headers: {Environment.NewLine}{String.Join(Environment.NewLine, interaction.RequestHeaders.Select(h => $"{h.Name}: {h.Value}"))}{Environment.NewLine} do not contain all the headers recorded in conversation for interaction {interaction.Number}: {Environment.NewLine}{String.Join(Environment.NewLine, recordedInteraction.RequestHeaders.Select(h=>$"{h.Name}: {h.Value}"))}.");
            }
            if (interaction.RequestContentType?.ToString() != recordedInteraction.RequestContentType?.ToString())
            {
                throw new ArgumentException($"HTTP request content type '{interaction.RequestContentType}' does not match method recorded in conversation for interaction {interaction.Number}, '{recordedInteraction.RequestContentType}'.");
            }
            if (interaction.RequestBody != recordedInteraction.RequestBody)
            {
                throw new ArgumentException($"HTTP request method '{interaction.RequestBody}' does not match method recorded in conversation for interaction {interaction.Number}, '{recordedInteraction.RequestBody}'.");
            }
            //Return completed task, no async logic required in the playback method
            var body = recordedInteraction.HasResponseBody ? Encoding.UTF8.GetBytes(recordedInteraction.ResponseBody) : null;
            return Task.FromResult(new ServiceResponse(body, recordedInteraction.ResponseContentType, recordedInteraction.StatusCode, recordedInteraction.ResponseHeaders));
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
