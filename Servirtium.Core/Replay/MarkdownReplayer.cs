using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Servirtium.Core;

namespace Servirtium.Core.Replay
{
    public class MarkdownReplayer : IInteractionMonitor
    {
        public static readonly string SERVIRTIUM_INTERACTION = "## Interaction ";
        //public static readonly Regex _interactionNumber = new Regex($"${Regex.Escape(SERVIRTIUM_INTERACTION)}([0-9]+).*");
        private string _filename = "no filename set";
        private IDictionary<int, IInteraction> _allInteractions;

        public MarkdownReplayer(IDictionary<int, IInteraction>? interactions = null)
        {
            _allInteractions = interactions ?? new Dictionary<int, IInteraction> { };
        }

        public Task<ServiceResponse> GetServiceResponseForRequest(Uri host, IInteraction interaction, bool lowerCaseHeaders)
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
                //throw new ArgumentException($"Fixed & filtered HTTP request headers: {Environment.NewLine}{interaction.RequestHeaders}{Environment.NewLine} do not contain all the headers recorded in conversation for interaction {interaction.Number}: {Environment.NewLine}{recordedInteraction.RequestHeaders}.");
            }
            if (interaction.RequestContentType != recordedInteraction.RequestContentType)
            {
                throw new ArgumentException($"HTTP request content type '{interaction.RequestContentType}' does not match method recorded in conversation for interaction {interaction.Number}, '{recordedInteraction.RequestContentType}'.");
            }
            if (interaction.RequestBody != recordedInteraction.RequestBody)
            {
                throw new ArgumentException($"HTTP request method '{interaction.RequestBody}' does not match method recorded in conversation for interaction {interaction.Number}, '{recordedInteraction.RequestBody}'.");
            }
            //Return completed task, no async logic required in the playback method
            return Task.FromResult(new ServiceResponse(recordedInteraction.ResponseBody, recordedInteraction.ResponseContentType, recordedInteraction.StatusCode, recordedInteraction.ResponseHeaders));
        }

        public IInteraction NewInteraction(int interactionNum, string context, string method, string path, string url)
        {
            throw new NotImplementedException();
        }

        public void LoadScriptFile(string filename) 
        {
            ReadPlaybackConversation(System.IO.File.ReadAllText(filename), filename);

        }

        public void ReadPlaybackConversation(string conversation, string filename = "no filename set")
        {
            _filename = filename;
            var interactionSections = conversation.Split(SERVIRTIUM_INTERACTION);
            if (interactionSections.Length < 2)
            {
                throw new ArgumentException($"No '{SERVIRTIUM_INTERACTION.Trim()}' found in conversation '{conversation} '. Wrong/empty script file?");
            }
            _allInteractions = interactionSections.Skip(1)
                .Select<string, IInteraction>(interactionText =>
                    new MarkdownInteraction.Builder()
                    .Markdown($"{SERVIRTIUM_INTERACTION}{interactionText}")
                    .Build())
                .ToDictionary(interaction=>interaction.Number, interaction=>interaction);
        }
    }
}
