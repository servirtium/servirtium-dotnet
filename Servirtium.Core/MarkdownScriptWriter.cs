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
        public void Write(TextWriter writer, IDictionary<int, IInteraction> interactions)
        {
            int finalInteractionNumber = interactions.Keys.Max();

            for (var i = 0; i <= finalInteractionNumber; i++)
            {
                if (interactions.TryGetValue(i, out var interaction))
                {
                    var markdown = $@"## Interaction {interaction.Number}: {interaction.Method} {interaction.Path}

### Request headers recorded for playback:

```{(interaction.RequestHeaders.Any() ? Environment.NewLine : "")}{String.Join(Environment.NewLine, interaction.RequestHeaders.Select(headerTuple => $"{headerTuple.Item1}: {headerTuple.Item2}"))}
```

### Request body recorded for playback ({interaction.RequestContentType?.MediaType ?? ""}):

```{(interaction.RequestBody != null ? $"{Environment.NewLine}{interaction.RequestBody}" : "")}
```

### Response headers recorded for playback:

```{(interaction.ResponseHeaders.Any() ? Environment.NewLine : "")}{String.Join(Environment.NewLine, interaction.ResponseHeaders.Select(headerTuple => $"{headerTuple.Item1}: {headerTuple.Item2}"))}
```

### Response body recorded for playback ({interaction.StatusCode}: {interaction.ResponseContentType?.MediaType ?? ""}):

```{(interaction.ResponseBody != null ? $"{Environment.NewLine}{interaction.ResponseBody}" : "")}
```

";
                    writer.Write(markdown);
                }
                else
                {
                    Debug.Assert(false, $"Interaction number {i} was missing (final interaction number: {finalInteractionNumber}). The MarkdownScriptWriter requires a contiguously numbered set of interactions, starting at zero.");
                }
            }
        }
    }
}
