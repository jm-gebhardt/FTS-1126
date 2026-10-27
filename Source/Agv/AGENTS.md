# TransportOrder Project Family Guide

## Scope

These instructions apply only when working in immediate child directories whose names start with `TransportOrder`, including their descendants. For other service directories under this folder, follow the [ACC guide](../../AGENTS.md) and their own scoped instructions; the TransportOrder-specific rules below do not apply.

For host changes, also follow the more specific [TransportOrder host guide](TransportOrder/AGENTS.md). Shared SDK, dependency-version, licensing, and operational-safety rules remain in the ACC guide.

## Ownership

| Directory | Responsibility |
| --- | --- |
| [TransportOrder](TransportOrder/TransportOrder.csproj) | Executable host, startup, configuration, and dependency registration |
| [TransportOrder.Core](TransportOrder.Core/TransportOrder.Core.csproj) | Command/event handling, background services, and orchestration |
| [TransportOrder.Domain](TransportOrder.Domain/TransportOrder.Domain.csproj) | Domain models, business rules, service interfaces, and repository abstractions |
| [TransportOrder.Messages](TransportOrder.Messages/TransportOrder.Messages.csproj) | Published message contracts |
| [TransportOrder.Persistence.Ef](TransportOrder.Persistence.Ef/TransportOrder.Persistence.Ef.csproj) | EF contexts, mappings, repositories, and migrations |
| [TransportOrder.Routing](TransportOrder.Routing/TransportOrder.Routing.csproj) | Route-finding algorithms and routing models |
| [TransportOrder.WebApi](TransportOrder.WebApi/TransportOrder.WebApi.csproj) | HTTP endpoints, DTOs, mapping, and API-facing consumers |
| [TransportOrder.Test](TransportOrder.Test/TransportOrder.Test.csproj) | Tests and fixtures for the project family |

`TransportOrder.TrafficManagement` currently contains build artifacts, not a maintained project. Use the existing [domain traffic-management implementation](TransportOrder.Domain/Services/TrafficManagement) and [traffic-management tests](TransportOrder.Test/Services/TrafficManagement); do not edit `bin` or `obj` output or infer a new project from that directory name.

## Change Conventions

- Change behavior in its owning project, then update [host registrations](TransportOrder/TransportOrderHostBuilderExtensions.cs) when new consumers, services, repositories, or implementations require wiring. Do not put domain decisions into the composition root or duplicate them in controllers.
- Preserve existing message contracts and correlation conventions across Common.Messages and TransportOrder.Messages. Check producers, consumers, and API clients together when changing a contract; avoid adding dependencies on the executable host to shared models or contracts.
- Keep database changes in [Persistence.Ef](TransportOrder.Persistence.Ef/TransportOrder.Persistence.Ef.csproj). Its [migrations](TransportOrder.Persistence.Ef/Migrations) cover different contexts; identify the intended context and existing migration conventions before generating a change. Do not apply migrations merely to validate code.
- For state changes, inspect the current domain methods and neighboring tests. Historical diagrams in [Doku](../../Doku) may be outdated; update relevant documentation from the implementation, not the reverse.
- Add tests to the existing test project and reuse nearby fixtures. Keep domain tests focused on rules, consumer tests on message handling, and persistence tests on database behavior; do not start the full Docker stack for a unit-test change.
- For shared routing, scheduling, or traffic-management changes, test affected dispatch and consumer behavior as well as the algorithm itself. Preserve existing configuration-driven strategy selection and DI lifetimes.

## Focused Validation

Run these commands from `ACC/Source/Agv`. Build the affected project first; this host build covers its project references:

```bash
dotnet build TransportOrder/TransportOrder.csproj --configuration Release
dotnet test TransportOrder.Test/TransportOrder.Test.csproj --configuration Release --filter "FullyQualifiedName~<test-class-or-method>"
```

- Replace the filter placeholder with an actual class or method. For host/DI changes, use `FullyQualifiedName~TransportOrderHostBuilderTests`.
- For the complete TransportOrder test shard, run `dotnet test Tests.TransportOrder.slnf --configuration Release`. Check shard membership when adding test projects.
- Use [existing test helpers](TransportOrder.Test/Helper) and [consumer fixtures](TransportOrder.Test/Services/CommandHandler) rather than creating a parallel harness. Check the owning test's prerequisites before running persistence or integration coverage.
- Missing private-feed credentials or external services are environment blockers, not reasons to weaken validation or change repository configuration. Follow the [ACC integration-test guide](../Agv.IntegrationTest/README.md) only when full-system verification is required.