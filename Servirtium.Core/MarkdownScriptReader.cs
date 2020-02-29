using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace Servirtium.Core
{
    public class MarkdownScriptReader : IScriptReader
    {
        public static readonly string SERVIRTIUM_INTERACTION = "## Interaction ";

        //Parses markdown for a single interaction and captures the content into named capture groups
        private static readonly Regex INTERACTION_REGEX = new Regex(
            @"(?xm)
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
              ```(?:\r\n|\n|\r)(?<requestHeaders>[\w\W]*?)(?:\r\n|\n|\r)```                                                             # Request Headers                   ```
                                                                                                                                        # (captures 'requestHeaders')       Accept: text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2
                                                                                                                                        #                                   User-Agent: Servirtium-Testing
                                                                                                                                        #                                   Connection: keep-alive
                                                                                                                                        #                                   Host: example.com
                                                                                                                                        #                                   ```
              [\r\n]+                                                                                                                   #
              \#\#\#\s+Request\s+body\s+recorded\s+for\s+playback\s+\((?<requestContentType>[^\n\r]+)?\):                               # Request Body Title                ### Request body recorded for playback (text/plain):
              [\r\n]+                                                                                                                   # (captures 'requestContentType')
              ```(?:\r\n|\n|\r)(?<requestBody>[\w\W]*?)(?:\r\n|\n|\r)```                                                                # Request Body                      ```
                                                                                                                                        # (captures 'requestBody')          request body contents
                                                                                                                                        #                                   ```
              [\r\n]+
              \#\#\#\s+Response\s+headers\s+recorded\s+for\s+playback:                                                                    #Response Header Title              ### Response headers recorded for playback:
              [\r\n]+
              ```(?:\r\n|\n|\r)(?<responseHeaders>[\w\W]*?)(?:\r\n|\n|\r)```                                                            # Response Headers                  ```
                                                                                                                                        # (captures 'responseHeaders')      Content-Type: application/json
                                                                                                                                        #                                   Connection: keep-alive
                                                                                                                                        #                                   Transfer-Encoding: chunked
                                                                                                                                        #                                   ```
              [\r\n]+
              \#\#\#\s+Response\s+body\s+recorded\s+for\s+playback\s+\((?<statusCode>[0-9]+):\s+(?<responseContentType>[^\n\r]+)?\):    # Response Body Title               ### Response body recorded for playback (200: application/json):
              [\r\n]+                                                                                                                   # (captures 'statusCode' 
                                                                                                                                        # and 'responseContentType')
              ```(?:\r\n|\n|\r)(?<responseBody>[\w\W]*?)(?:\r\n|\n|\r)```                                                               # Response Body                     ```
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
        public IDictionary<int, IInteraction> Read(TextReader reader)
        {

            var conversation = reader.ReadToEnd();

            var interactionSections = conversation.Split(SERVIRTIUM_INTERACTION);
            if (interactionSections.Length < 2)
            {
                throw new ArgumentException($"No '{SERVIRTIUM_INTERACTION.Trim()}' found in conversation '{conversation} '. Wrong/empty script file?");
            }

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
                                    content = content.Substring(4, content.Length - 8);
                                }
                                else if (content.StartsWith("```\r\n") && content.EndsWith("\r\n```"))
                                {
                                    contentType = IInteraction.Note.NoteType.Code;
                                    content = content.Substring(5, content.Length - 10);
                                }
                                return new IInteraction.Note(contentType, c.Value, content);
                            })
                            : new IInteraction.Note[0];
                        var builder = new ImmutableInteraction.Builder()
                            .Number(Int32.Parse(match.Groups["number"].Value))
                            .Notes(notes)
                            .Method(new HttpMethod(match.Groups["method"].Value))
                            .Path(match.Groups["path"].Value)
                            .RequestHeaders(HeaderTextToHeaderList(match.Groups["requestHeaders"].Value))
                            .ResponseHeaders(HeaderTextToHeaderList(match.Groups["responseHeaders"].Value))
                            .StatusCode(Enum.Parse<HttpStatusCode>(match.Groups["statusCode"].Value));

                        var requestBody = match.Groups["requestBody"].Value;
                        if (match.Groups["requestContentType"].Success && requestBody.Any())
                        {
                            builder.RequestBody(requestBody, MediaTypeHeaderValue.Parse(match.Groups["requestContentType"].Value));
                        }

                        var responseBody = match.Groups["responseBody"].Value;
                        if (match.Groups["responseContentType"].Success && responseBody.Any())
                        {
                            builder.ResponseBody(responseBody, MediaTypeHeaderValue.Parse(match.Groups["responseContentType"].Value));
                        }
                        return builder.Build();
                    })
                .ToDictionary(interaction => interaction.Number, interaction => interaction);



            return allInteractions;
        }
    }
}
