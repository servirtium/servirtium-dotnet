# Servirtium .NET

.NET Core implementation of [Servirtium](https://servirtium.dev), based on the Java implementation: https://github.com/servirtium/servirtium-java. Development led by [Stephen Hand](https://twitter.com/HandStephen).

## Examples of Use

You're going to use this with a Unit test framework like NUnit. Most likely you're writing will be "service tests" which are part of the class of tests called "integration tests".

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

See full demo project for more complete example code: [servirtium/demo-dotnet-climate-tck](https://github.com/servirtium/demo-dotnet-climate-tck)

## CLI Commands

### Build This Solution

You will need to have the .NET core SDK 6 installed. From the solution directory:

```
dotnet restore
dotnet build
````

### Running All Tests

From the solution directory:

`dotnet test`

### Publishing to your localhost Nuget for development purposes

```
rm -rf ~/.nuget/packages/servirtium*
rm -rf ~/.nuget/packages/Servirtium*
dotnet pack -p:PackageVersion=1.5.0-dev --output ~/.nuget/packages/
```

## Published NuGet assemblies

* [Servirtium.Core/1.4.0](https://www.nuget.org/packages/Servirtium.Core/1.4.0)
* [Servirtium.AspNetCore/1.4.0](https://www.nuget.org/packages/Servirtium.AspNetCore/1.4.0)

## Current Status

The Servirtium implementation guide is complete and all features are implemented: [servirtium.dev/new](https://servirtium.dev/new)

## Roadmap

Current roadmap in priority order:

* Productionize standalone server host executable (currently used to sanity check HTTP requests from tests against those sent from postman) to offer a subset of Servirtium functionality out of process.

## Confirming compatability with other implementations

TODO - ph

Read about the [compatibility suite](COMPATIBILITY_SUITE.md) for this .NET implementation

## Releasing New Versions

Releases are pushed using the `release-package.yml` Github action, which is triggered by creating a Github release

To publish a new version of the Servirtium.Core package:

1. Create & Publish a new Github release from the web UI, with the tag Servirtium.Core/v<semver2-version> - the tag can be created in advance or be new as part of the release, but the package will only be pushed when a release is created in either case.
2. The release-package.yml workflow should pick up this new release and push a nuget package.

To publish a new version of the Servirtium.AspNetCore package:

1. Create & Publish a new Github release from the web UI, with the tag `Servirtium.AspNetCore/v<semver2-version>`- the tag can be created in advance or be new as part of the release, but the package will only be pushed when a release is created in either case.
2. The release-package.yml workflow should pick up this new release and push a nuget package.

The release-package.yml workflow is generic, it will publish any package you name prior to the forward slash, provided there is a directory in the root of the source with that name containing a csproj file with the same name.

e.g. You could create a release tagged `Apoplectic.Turkeys/v1.2.3` - if there is a .NET project at `/Apoplectic.Turkeys/Apoplectic.Turkeys.csproj` in the source, it will pack and push package Apoplectic.Turkeys version 1.2.3. Otherwise the workflow will fail and nothing will happen. See the `.github/` folder.