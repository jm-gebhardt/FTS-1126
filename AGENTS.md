# ACC Backend Guide

## Implementation

- ACC is a .NET microservice system. Use the [service overview](README.md), [module interactions](Doku/ModuleInteraction.md), and [architecture decisions](Doku/adrs/0001-readme.md) before changing contracts or state transitions.
- Preserve each service's existing domain, messages, EF persistence, Web API, and test boundaries. Prefer the owning service over adding behavior to shared Common code.
- [Shared project settings](Source/Agv/SharedProjectSettings.csproj) target `net10.0`, enable nullable reference types, and treat warnings as errors. Use a compatible .NET SDK; do not lower these checks to make a build pass.
- Manage dependency versions in [Directory.Packages.props](Source/Directory.Packages.props). Respect its licensing locks on AutoMapper and MassTransit and its coordinated observability and notification-center updates.
- Keep relevant state-machine diagrams in `Doku` aligned with behavioral changes. Follow the [backend PR checklist](pull_request_template.md) for tests, migrations, and documentation.

## Validation

Run commands from this directory. Private NuGet feeds may require developer-provided authentication; never embed tokens or replace feed configuration to bypass restore errors.

- Build: `dotnet build Source/Agv/CompileAgv.sln --configuration Release`.
- Prefer `dotnet test <affected-test-project.csproj> --configuration Release` for a narrow change. Unit tests use MSTest; reuse adjacent helpers and assertions.
- Broader unit-test shards: `dotnet test Source/Agv/Tests.TransportOrder.slnf --configuration Release` and `dotnet test Source/Agv/Tests.Others.slnf --configuration Release`. When adding a test project, check solution and shard membership to avoid omissions or duplication.
- Use [coverlet settings](coverlet.runsettings) when collecting coverage rather than inventing exclusions.
- Integration tests start the full Docker system and need registry/environment setup. Read the [integration-test guide](Source/Agv.IntegrationTest/README.md) first; they are not a lightweight unit-test substitute. Its REST client is generated at build time from the checked-in Swagger input.