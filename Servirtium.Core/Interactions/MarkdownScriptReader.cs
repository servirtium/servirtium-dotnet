using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Servirtium.Core.Interactions
{
    public class MarkdownScriptReader : IScriptReader
    {
        private static readonly string SERVIRTIUM_INTERACTION = "## Interaction ";

        private static string GetCodeBlockPattern(string captureName) =>
            $@"(?:
            (?:```(?:\r\n|\n|\r)(?<{captureName}>[\w\W]*?)(?:\r\n|\n|\r)```)|
            (?:(\s\s\s\s(?<{captureName}>[\w\W]*?(?:\r\n|\n|\r)))+)
)";
        private static string CodeBlockContents(CaptureCollection codeBlockLines) => String.Join("", codeBlockLines).TrimEnd('\r', '\n');

        private static readonly Regex INDENTED_CODE_BLOCK_NEWLINE = new Regex(@"((?:\r\n)|\n|\r)    ", RegexOptions.Compiled);

        //Parses markdown for a single interaction and captures the content into named capture groups
        private static readonly Regex INTERACTION_REGEX = new Regex(
            $@"(?xm)
              \#\#\s+Interaction\s+(?<number>[0-9]+):\s+(?:\*|_)?(?<method>[A-Za-z]+)(?:\*|_)?\s+(?<path>[^\n\r]+)                       # Interaction title                 ## Interaction 0: POST /my-api/rest/v1/stuff
              [\r\n]+                                                                                                                   # (captures 'method' and 'path')
              (?:                                                                                                                       # Overall capture for each note section. 'noteTitle' and 'noteContent' named groups can capture multiple values
              \#\#\s\[Note\]\s(?<noteTitle>[^\n\r]+)?:                                                                                  # Match the heading and extract the title
              (?:\r\n|\n|\r)                                                                                                            #
              (?:\r\n|\n|\r)                                                                                                            #
              (?<noteContent>.*?)                                                                                                       # Captures raw content. Type will be determined later based on if it is in a code block or not
              [\r\n]+
              )*
              \#\#\#\s+Request\s+headers\s+recorded\s+for\s+playback:                                                                   # Request Header Title              ### Request headers recorded for playback: 
              [\r\n]+                                                                                                                   #
              {GetCodeBlockPattern("requestHeaders")}                                                                                   # Request Headers                   ```
                                                                                                                                        # (captures 'requestHeaders')       Accept: text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2
                                                                                                                                        #                                   User-Agent: Servirtium-Testing
                                                                                                                                        #                                   Connection: keep-alive
                                                                                                                                        #                                   Host: example.com
                                                                                                                                        #                                   ```
              [\r\n]+                                                                                                                   #
              \#\#\#\s+Request\s+body\s+recorded\s+for\s+playback\s+\((?<requestContentType>[^\n\r]+)?\):                               # Request Body Title                ### Request body recorded for playback (text/plain):
              [\r\n]+                                                                                                                   # (captures 'requestContentType')
              {GetCodeBlockPattern("requestBody")}                                                                                      # Request Body                      ```
                                                                                                                                        # (captures 'requestBody')          request body contents
                                                                                                                                        #                                   ```
              [\r\n]+
              \#\#\#\s+Response\s+headers\s+recorded\s+for\s+playback:                                                                    #Response Header Title              ### Response headers recorded for playback:
              [\r\n]+
              {GetCodeBlockPattern("responseHeaders")}                                                                                  # Response Headers                  ```
                                                                                                                                        # (captures 'responseHeaders')      Content-Type: application/json
                                                                                                                                        #                                   Connection: keep-alive
                                                                                                                                        #                                   Transfer-Encoding: chunked
                                                                                                                                        #                                   ```
              [\r\n]+
              \#\#\#\s+Response\s+body\s+recorded\s+for\s+playback\s+\((?<statusCode>[0-9]+):\s+(?<responseContentType>[^\n\r]+)?\):    # Response Body Title               ### Response body recorded for playback (200: application/json):
              [\r\n]+                                                                                                                   # (captures 'statusCode' 
                                                                                                                                        # and 'responseContentType')
              {GetCodeBlockPattern("responseBody")}                                                                                     # Response Body                     ```
                                                                                                                                        # (captures 'responseBody')         response body contents
                                                                                                                                        #                                   ```
            "
        , RegexOptions.Singleline | RegexOptions.Compiled);

        private static IEnumerable<(string, string)> HeaderTextToHeaderList(string headerText)
           => headerText.Split('\n')
                //remove empty lines first
                .Where(line => !String.IsNullOrWhiteSpace(line))
                .Select(line => {
                    var bits = line.Split(":");
                    if (bits.Length < 2)
                    {
                        throw new ArgumentException($"This headers contain a line that was not formatted as a valid HTTP header (i.e. <NAME>: <VALUE>): {headerText}", nameof(headerText));
                    }
                    return (bits[0].Trim(), String.Join(":", bits.Skip(1)).Trim());
                });

        private readonly ILogger<MarkdownScriptReader> _logger;

        public MarkdownScriptReader(ILoggerFactory? loggerFactory = null)
        {
            _logger = (loggerFactory ?? NullLoggerFactory.Instance)
                .CreateLogger<MarkdownScriptReader>();
        }
        
        public IDictionary<int, IInteraction> Read(TextReader reader)
        {

            var conversation = reader.ReadToEnd();

            var interactionSections = conversation.Split(SERVIRTIUM_INTERACTION);
            if (interactionSections.Length < 2)
            {
                throw new ArgumentException($"No '{SERVIRTIUM_INTERACTION.Trim()}' found in conversation '{conversation} '. Wrong/empty script file?");
            }
            _logger.LogDebug($"Loading conversation of {interactionSections.Length-1} interactions.");
            var allInteractions = interactionSections.Skip(1)
                .Select<string, IInteraction>(interactionMarkdown =>
                    {
                        //Put the header removed in the 'Split' operation back to how it was;
                        interactionMarkdown = $"{SERVIRTIUM_INTERACTION}{interactionMarkdown}";
                        var match = INTERACTION_REGEX.Match(interactionMarkdown);
                        if (!match.Success)
                        {
                            throw new ArgumentException($"This markdown could not be parsed as an interaction: {interactionMarkdown}");
                        }

                        //Parse captured notes & determine if they are code or text
                        var notes = match.Groups["noteTitle"].Success ?
                            match.Groups["noteTitle"].Captures.Select((c, idx) =>
                            {
                                var content = match.Groups["noteContent"].Captures[idx].Value;
                                var contentType = IInteraction.Note.NoteType.Text;
                                //Need to check for both types of line ending regardless of platform (no, it's not chacking for 90's Mac line endings)
                                if (content.StartsWith("```\n") && content.EndsWith("\n```"))
                                {
                                    contentType = IInteraction.Note.NoteType.Code;
                                    content = content[4..^4];
                                }
                                else if (content.StartsWith("```\r\n") && content.EndsWith("\r\n```"))
                                {
                                    contentType = IInteraction.Note.NoteType.Code;
                                    content = content[5..^5];
                                }
                                else if (content.StartsWith("    "))
                                {
                                    contentType = IInteraction.Note.NoteType.Code;
                                    content = INDENTED_CODE_BLOCK_NEWLINE.Replace(content[4..], "$1");
                                }
                                return new IInteraction.Note(contentType, c.Value, content);
                            })
                            : new IInteraction.Note[0];
                        var builder = new ImmutableInteraction.Builder()
                            .Number(Int32.Parse(match.Groups["number"].Value))
                            .Notes(notes)
                            .Method(new HttpMethod(match.Groups["method"].Value))
                            .Path(match.Groups["path"].Value)
                            .RequestHeaders(HeaderTextToHeaderList(CodeBlockContents(match.Groups["requestHeaders"].Captures)))
                            .ResponseHeaders(HeaderTextToHeaderList(CodeBlockContents(match.Groups["responseHeaders"].Captures)))
                            .StatusCode(Enum.Parse<HttpStatusCode>(match.Groups["statusCode"].Value));
                        
                        
                        var requestBody = CodeBlockContents(match.Groups["requestBody"].Captures);
                        if (match.Groups["requestContentType"].Success)
                        {
                            builder.RequestBody(requestBody, MediaTypeHeaderValue.Parse(match.Groups["requestContentType"].Value));
                        }

                        var responseBody = CodeBlockContents(match.Groups["responseBody"].Captures);
                        if (match.Groups["responseContentType"].Success)
                        {
                            builder.ResponseBody(responseBody, MediaTypeHeaderValue.Parse(match.Groups["responseContentType"].Value));
                        }
                        var interaction = builder.Build();
                        _logger.LogDebug($"Read interaction {interaction.Number}, a {interaction.Method} request to {interaction.Path}, returning {interaction.StatusCode}{(interaction.Notes.Any() ? $", with {interaction.Notes.Count()} notes." : ".")}");
                        return interaction;
                    })
                .ToDictionary(interaction => interaction.Number, interaction => interaction);



            return allInteractions;
        }

    }
}
