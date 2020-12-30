using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NewRegexReplacement = Servirtium.Core.RegexReplacement;
using NewReplacementContext = Servirtium.Core.ReplacementContext;

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

        private readonly IEnumerable<NewRegexReplacement> _replacementsForRecording;

        private readonly IScriptWriter _delegateScriptWriter;

        private readonly ILogger<FindAndReplaceScriptWriter> _logger;

        [Obsolete("Use top level RegexReplacement struct instead")]
        public FindAndReplaceScriptWriter(IEnumerable<RegexReplacement> replacementsForRecording, IScriptWriter delegateScriptWriter, ILoggerFactory? loggerFactory = null)
            :this(replacementsForRecording.Select(rr => new NewRegexReplacement(rr.Matcher, rr.Replacement, (NewReplacementContext)rr.Context)), delegateScriptWriter, loggerFactory)
        { }

        public FindAndReplaceScriptWriter(IEnumerable<NewRegexReplacement> replacementsForRecording, IScriptWriter delegateScriptWriter, ILoggerFactory? loggerFactory = null)
        {
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<FindAndReplaceScriptWriter>();
            _replacementsForRecording = replacementsForRecording;
            _delegateScriptWriter = delegateScriptWriter;
        }

        private IInteraction TransformInteraction(IInteraction interaction)
        {
            var updatedRequestHeaders = interaction.RequestHeaders
    .Select(h => NewRegexReplacement.FixHeaderForRecording(h, _replacementsForRecording, NewReplacementContext.RequestHeader));
            var updatedResponseHeaders = interaction.ResponseHeaders
                .Select(h => NewRegexReplacement.FixHeaderForRecording(h, _replacementsForRecording, NewReplacementContext.ResponseHeader));
            var builder = new ImmutableInteraction.Builder()
                .From(interaction)
                .RequestHeaders(updatedRequestHeaders)
                .ResponseHeaders(updatedResponseHeaders);
            if (interaction.RequestBody.HasValue)
            {
                var (content, type) = interaction.RequestBody.Value;
                builder.RequestBody(NewRegexReplacement.FixStringForRecording(content, _replacementsForRecording, NewReplacementContext.RequestBody), type);
            }
            if (interaction.ResponseBody.HasValue)
            {
                var (content, type) = interaction.ResponseBody.Value;
                builder.ResponseBody(NewRegexReplacement.FixStringForRecording(content, _replacementsForRecording, NewReplacementContext.ResponseBody), type);
            }

            var transformed = (IInteraction)builder.Build();
            _logger.LogDebug($"Performed find & replace sweep on interaction {transformed.Number}, a {interaction.Method} request to {transformed.Path} returning {transformed.StatusCode}.");
            return transformed;
        }

        public void Write(TextWriter writer, IDictionary<int, IInteraction> interactions)
        {
            _logger.LogDebug($"Performing find & replace sweep on {interactions.Count} interactions with {_replacementsForRecording.Count()} replacements.");
            var interactionsToRecord = interactions.ToDictionary(kvp => kvp.Key, kvp => TransformInteraction(kvp.Value));
            _delegateScriptWriter.Write(writer, interactionsToRecord);
        }
    }
}
