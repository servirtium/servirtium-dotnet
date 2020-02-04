using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Servirtium.Core
{
    //Wraps another script writer. Copies the input interactions with regex based find & replace updates and then passes them down to the delegate writer 
    public class FindAndReplaceScriptWriter : IScriptWriter
    {
        [Flags]
        public enum ReplacementContext
        {
            RequestHeader,
            RequestBody,
            ResponseHeader
        }

        public struct RegexReplacement
        {
            public RegexReplacement(Regex matcher, string replacement, ReplacementContext context = ReplacementContext.RequestBody | ReplacementContext.RequestHeader | ReplacementContext.ResponseHeader)
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
            var fixedHeaderText = FixStringForRecording($"{name}: {val}", regexReplacements
                .Where(rr => (rr.Context & ReplacementContext.ResponseHeader) == ReplacementContext.ResponseHeader));
            var headerBits = fixedHeaderText.Split(':', 2);
            return (headerBits[0], headerBits[1]);
        }

        public FindAndReplaceScriptWriter(IEnumerable<RegexReplacement> replacementsForRecording, IScriptWriter delegateScriptWriter)
        {
            _replacementsForRecording = replacementsForRecording;
            _delegateScriptWriter = delegateScriptWriter;
        }

        public void Write(TextWriter writer, IDictionary<int, IInteraction> interactions)
        {
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
                if (interaction.HasRequestBody)
                {
                    builder.RequestBody(FixStringForRecording(interaction.RequestBody!, _replacementsForRecording
                            .Where(rr => (rr.Context & ReplacementContext.RequestBody) == ReplacementContext.RequestBody)), interaction.RequestContentType!);
                }

                return (IInteraction)builder.Build();
            });
            _delegateScriptWriter.Write(writer, interactionsToRecord);
        }
    }
}
