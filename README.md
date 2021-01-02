# Servirtium .NET

.NET Core 3.1 implementation of Servirtium, based on the Java implementation: https://github.com/servirtium/servirtium-java

## Examples

### Recording HTTP interactions to Servirtium Markdown

```csharp
            // for your SetUp method in NUnit ...

            var recorder = new MarkdownRecorder(
                ClimateApi.DEFAULT_SITE, $@"test_recordings\{script}",
                new FindAndReplaceScriptWriter(new[] {
                    new RegexReplacement(new Regex("User-Agent: .*"), "User-Agent: Servirtium-Testing")
                }, new MarkdownScriptWriter()));

            var server = AspNetCoreServirtiumServer.WithTransforms(
                1234,
                recorder,
                new SimpleInteractionTransforms(
                    ClimateApi.DEFAULT_SITE,
                    new Regex[0],
                    new[] {
                    "Date:", "X-", "Strict-Transport-Security",
                    "Content-Security-Policy", "Cache-Control", "Secure", "HttpOnly",
                    "Set-Cookie: climatedata.cookie=" }.Select(pattern => new Regex(pattern))
                ));

            server.start();
            
            //Some tests to record interactions using Servirtium on host 'localhost:1234'
            // TODO

            // for your TearDown method in NUnit ...
            server.stop();
```

### Replaying HTTP interactions from Servirtium Markdown

```csharp
            // for your SetUp method in NUnit ...

            var replayer = new MarkdownReplayer();
            replayer.LoadScriptFile($@"test_recordings\{script}");

            AspNetCoreServirtiumServer.WithTransforms(
                1234,
                replayer,
                new SimpleInteractionTransforms(
                    ClimateApi.DEFAULT_SITE,
                    new Regex[0],
                    new[] { new Regex("Date:") }
                )),

            server.start();
            
            //Some tests to record interactions using Servirtium on host 'localhost:1234'
            // TODO

            // for your TearDown method in NUnit ...
            server.stop();
```

See full demo project for more complete example code: https://github.com/servirtium/demo-dotnet-climate-tck

## CLI Commands

### Build Solution

From the solution directory:

`dotnet build`

### Run All Tests

From the solution directory:

`dotnet test`

### Published NuGet assemblies

* [Servirtium.Core/1.0.0](https://www.nuget.org/packages/Servirtium.Core/1.0.0)
* [Servirtium.AspNetCore/1.0.0](https://www.nuget.org/packages/Servirtium.AspNetCore/1.0.0)

## Current Status

The Servirtium implementation guide is complete and all features are implemented: [starting-a-new-implementation.md](https://github.com/servirtium/README/blob/master/starting-a-new-implementation.md)

## Roadmap

Current roadmap in priority order:

* Productionise standalone server host executable (currently used to sanity check HTTP requests from tests against those sent from postman) to offer a subset of Servirtium functionality out of process.

## Confirming compatability with other implementations

See https://github.com/servirtium/compatibility-suite-runner