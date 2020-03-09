using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Servirtium.Core.Interactions
{
    //Wraps another script writer. Copies the input interactions with regex based find & replace updates and then passes them down to the delegate writer 
    public class FindAndReplaceScriptWriter : IScriptWriter
    {
        [Flags]
        public enum ReplacementContext
        {
            None = 0,
            RequestHeader = 1,
            RequestBody = 2,
            ResponseHeader = 4,
            ResponseBody = 8
        }

        public readonly struct RegexReplacement
        {
            public RegexReplacement(Regex matcher, string replacement, ReplacementContext context = (ReplacementContext.RequestBody | ReplacementContext.RequestHeader | ReplacementContext.ResponseHeader | ReplacementContext.ResponseBody))
            {
                Matcher = matcher;
                Replacement = replacement;
                Context = context;
            }

            public Regex Matcher { get; }
            public string Replacement { get; }
            public ReplacementContext Context { get; }
        }

        private readonly IEnumerable<RegexReplacement> _replacementsForRecording;

        private readonly IScriptWriter _delegateScriptWriter;

        private static string FixStringForRecording(string input, IEnumerable<RegexReplacement> regexReplacements)
        {
            var output = input;
            foreach (var regexReplacement in regexReplacements)
            {
                output = regexReplacement.Matcher.Replace(output, regexReplacement.Replacement);
            }
            return output;
        }

        private static (string, string) FixHeaderForRecording((string, string) input, IEnumerable<RegexReplacement> regexReplacements)
        {
            (string name, string val) = input;
            var fixedHeaderText = FixStringForRecording($"{name}: {val}", regexReplacements);
            var headerBits = fixedHeaderText.Split(": ", 2);
            return (headerBits[0], headerBits[1]);
        }

        private readonly ILogger<FindAndReplaceScriptWriter> _logger;

        public FindAndReplaceScriptWriter(IEnumerable<RegexReplacement> replacementsForRecording, IScriptWriter delegateScriptWriter, ILoggerFactory? loggerFactory = null)
        {
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<FindAndReplaceScriptWriter>();
            _replacementsForRecording = replacementsForRecording;
            _delegateScriptWriter = delegateScriptWriter;
        }

        public void Write(TextWriter writer, IDictionary<int, IInteraction> interactions)
        {
            _logger.LogDebug($"Performing find & replace sweep on {interactions.Count} interactions with {_replacementsForRecording.Count()} replacements.");
            var interactionsToRecord = interactions.ToDictionary(kvp => kvp.Key, kvp =>
            {
                var interaction = kvp.Value;
                var updatedRequestHeaders = interaction.RequestHeaders
                    .Select(h => FixHeaderForRecording(h, _replacementsForRecording
                            .Where(rr => (rr.Context & ReplacementContext.RequestHeader) == ReplacementContext.RequestHeader)));
                var updatedResponseHeaders = interaction.ResponseHeaders
                    .Select(h => FixHeaderForRecording(h, _replacementsForRecording
                            .Where(rr => (rr.Context & ReplacementContext.ResponseHeader) == ReplacementContext.ResponseHeader)));
                var builder = new ImmutableInteraction.Builder()
                    .From(interaction)
                    .RequestHeaders(updatedRequestHeaders)
                    .ResponseHeaders(updatedResponseHeaders);
                if (interaction.RequestBody.HasValue)
                {
                    var (content, type) = interaction.RequestBody.Value;
                    builder.RequestBody(FixStringForRecording(content, _replacementsForRecording
                            .Where(rr => (rr.Context & ReplacementContext.RequestBody) == ReplacementContext.RequestBody)), type);
                }
                if (interaction.ResponseBody.HasValue)
                {
                    var (content, type) = interaction.ResponseBody.Value;
                    builder.ResponseBody(FixStringForRecording(content, _replacementsForRecording
                        .Where(rr => (rr.Context & ReplacementContext.ResponseBody) == ReplacementContext.ResponseBody)), type);
                }

                var transformed = (IInteraction)builder.Build();
                _logger.LogDebug($"Performed find & replace sweep on interaction {transformed.Number}, a {interaction.Method} request to {transformed.Path} returning {transformed.StatusCode}.");
                return transformed;
            });
            _delegateScriptWriter.Write(writer, interactionsToRecord);
        }
    }
}
