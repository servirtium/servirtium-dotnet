#!/bin/sh

set -e

rm -rf ~/.nuget/packages/servirtium.*
nuget add Servirtium.Core/bin/Debug/Servirtium.Core.1.2.1-dev.1.nupkg -source ~/.nuget/packages
nuget add Servirtium.StandaloneServer/bin/Debug/Servirtium.StandaloneServer.1.2.1-dev.1.nupkg -source ~/.nuget/packages
nuget add Servirtium.AspNetCore/bin/Debug/Servirtium.AspNetCore.1.2.1-dev.1.nupkg -source ~/.nuget/packages
