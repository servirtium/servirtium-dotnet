using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Servirtium.Core.Interactions
{
    public class MarkdownScriptWriter : IScriptWriter
    {

        public enum CodeblockDemarcation
        {
            TripleBacktick,
            FourSpaceIndent
        }
        public class Settings {
            public CodeblockDemarcation CodeblockDemarcation = CodeblockDemarcation.TripleBacktick;
            public bool EmphasiseHttpVerbs  = false;
        }

        private readonly ILogger<MarkdownScriptWriter> _logger;
        
        private readonly Settings _settings;

        public MarkdownScriptWriter(Settings? settings = null, ILoggerFactory? loggerFactory = null)
        {
            _logger = (loggerFactory ?? NullLoggerFactory.Instance)
                .CreateLogger<MarkdownScriptWriter>();
            this._settings = settings ?? new Settings();
        }

        private static readonly Regex _newLine = new Regex(@"(?:\r\n)|\n|\r", RegexOptions.Compiled);

        private string WrapInCodeBlock(string? optionalCode)
        {
            string code = optionalCode ?? "";
            switch (_settings.CodeblockDemarcation)
            {
                case CodeblockDemarcation.FourSpaceIndent:
                    {
                        return $"    {_newLine.Replace(code, "$0    ")}";
                    }
                case CodeblockDemarcation.TripleBacktick:
                default:
                    {
                        return $@"```
{code}
```";
                    }

            }
        }

        public void Write(TextWriter writer, IDictionary<int, IInteraction> interactions)
        {
            (int start, int end) interactionBounds = interactions.Keys.Any() ? (interactions.Keys.Min(), interactions.Keys.Max()) : (0, -1);
            int startInteractionNumber = interactions.Keys.Any() ? interactions.Keys.Max() : -1;
            _logger.LogDebug($"Recording {interactionBounds.end+1} interactions");
            
            for (var i = interactionBounds.start; i <= interactionBounds.end; i++)
            {
                if (interactions.TryGetValue(i, out var interaction))
                {
                    if (interaction.Number != i)
                    {
                        throw new ArgumentException($"Interaction at dictionary key '{i}' is number '{interaction.Number}'. The dictionary should always be keyed on the interaction's nuymber or it cannot be written.");
                    }
                    WriteInteraction(writer, interaction);
                }
                else
                {
                    throw new MissingInteractionException($"Interaction number {i} was missing (final interaction number: {interactionBounds.end}). The MarkdownScriptWriter requires a contiguously numbered set of interactions, starting at zero.", i);
                }
            }
            _logger.LogInformation($"Recorded {interactionBounds.end + 1} interactions");
        }

        private void WriteInteraction(TextWriter writer, IInteraction interaction)
        {
            var noteMarkdown = String.Join("", interaction.Notes.Select(n => {
                var noteContentHeader = n.Type == IInteraction.Note.NoteType.Code ? $"```{Environment.NewLine}" : "";
                var noteContentFooter = n.Type == IInteraction.Note.NoteType.Code ? $"{Environment.NewLine}```" : "";
                return $@"## [Note] {n.Title}:

{(n.Type == IInteraction.Note.NoteType.Code ? WrapInCodeBlock(n.Content) : n.Content)}

";
            }));
            string requestContent = "", requestType = "";
            if (interaction.RequestBody.HasValue)
            {
                var (content, type) = interaction.RequestBody.Value;
                requestContent = content;
                requestType = type.ToString();
            }
            string responseContent = "", responseType = "";
            if (interaction.ResponseBody.HasValue)
            {
                var (content, type) = interaction.ResponseBody.Value;
                responseContent = content;
                responseType = type.ToString();
            }

            var httpMethodMarkdown = _settings.EmphasiseHttpVerbs ? $"*{interaction.Method}*" : interaction.Method.ToString();
            var markdown = $@"## Interaction {interaction.Number}: {httpMethodMarkdown} {interaction.Path}

{noteMarkdown}### Request headers recorded for playback:

{WrapInCodeBlock(String.Join(Environment.NewLine, interaction.RequestHeaders.Select(headerTuple => $"{headerTuple.Item1}: {headerTuple.Item2}")))}

### Request body recorded for playback ({requestType}):

{WrapInCodeBlock(requestContent)}

### Response headers recorded for playback:

{WrapInCodeBlock(String.Join(Environment.NewLine, interaction.ResponseHeaders.Select(headerTuple => $"{headerTuple.Item1}: {headerTuple.Item2}")))}

### Response body recorded for playback ({(int)interaction.StatusCode}: {responseType}):

{WrapInCodeBlock(responseContent)}

";
            writer.Write(markdown);
            _logger.LogDebug($"Wrote interaction {interaction.Number}, A {interaction.Method} request to {interaction.Path}, returning {interaction.StatusCode}{(interaction.Notes.Any() ? $", with {interaction.Notes.Count()} notes." : ".")}");

        }
    }
}
