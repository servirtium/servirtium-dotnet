using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Servirtium.Core
{
    public class MarkdownScriptWriter : IScriptWriter
    {
        private readonly IBodyFormatter _requestBodyFormatter, _responseBodyFormatter;
        public void Write(TextWriter writer, IDictionary<int, IInteraction> interactions)
        {
            int finalInteractionNumber = interactions.Keys.Max();

            for (var i = 0; i <= finalInteractionNumber; i++)
            {
                if (interactions.TryGetValue(i, out var interaction))
                {
                    if (interaction.Number != i)
                    {
                        throw new ArgumentException($"Interaction at dictionary key '{i}' is number '{interaction.Number}'. The dictionary should always be keyed on the interaction's nuymber or it cannot be written.");
                    }
                    var noteMarkdown = String.Join("", interaction.Notes.Select(n => {
                        var noteContentHeader = n.Type == IInteraction.Note.NoteType.Code ? $"```{Environment.NewLine}" : "";
                        var noteContentFooter = n.Type == IInteraction.Note.NoteType.Code ? $"{Environment.NewLine}```" : "";
                        return $@"## [Note] {n.Title}:

{noteContentHeader}{n.Content}{noteContentFooter}

";
                    }));

                    var markdown = $@"## Interaction {interaction.Number}: {interaction.Method} {interaction.Path}

{noteMarkdown}### Request headers recorded for playback:

```
{String.Join(Environment.NewLine, interaction.RequestHeaders.Select(headerTuple => $"{headerTuple.Item1}: {headerTuple.Item2}"))}
```

### Request body recorded for playback ({interaction.RequestContentType?.ToString() ?? ""}):

```
{interaction.RequestBody}
```

### Response headers recorded for playback:

```
{String.Join(Environment.NewLine, interaction.ResponseHeaders.Select(headerTuple => $"{headerTuple.Item1}: {headerTuple.Item2}"))}
```

### Response body recorded for playback ({(int)interaction.StatusCode}: {interaction.ResponseContentType?.ToString() ?? ""}):

```
{interaction.ResponseBody}
```

";
                    writer.Write(markdown);
                }
                else
                {
                    throw new ArgumentException($"Interaction number {i} was missing (final interaction number: {finalInteractionNumber}). The MarkdownScriptWriter requires a contiguously numbered set of interactions, starting at zero.");
                }
            }
        }
    }
}
