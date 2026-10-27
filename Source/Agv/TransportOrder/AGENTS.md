# TransportOrder Host Guide

## Scope and Ownership

- Follow the [ACC guide](../../../AGENTS.md). This directory is the executable host and composition root, not the home of all TransportOrder business logic.
- Keep startup and registration changes here; put handlers and orchestration in [TransportOrder.Core](../TransportOrder.Core/TransportOrder.Core.csproj), domain rules in [TransportOrder.Domain](../TransportOrder.Domain/TransportOrder.Domain.csproj), database implementations in [TransportOrder.Persistence.Ef](../TransportOrder.Persistence.Ef/TransportOrder.Persistence.Ef.csproj), routing algorithms in [TransportOrder.Routing](../TransportOrder.Routing/TransportOrder.Routing.csproj), and endpoints in [TransportOrder.WebApi](../TransportOrder.WebApi/TransportOrder.WebApi.csproj).
- Check existing contracts in [TransportOrder.Messages](../TransportOrder.Messages/TransportOrder.Messages.csproj) and Common.Messages before introducing a message. Contract changes can affect device communication, upstream services, and KITT clients.

## Registration and Runtime Behavior

- Use [TransportOrderHostBuilderExtensions](TransportOrderHostBuilderExtensions.cs) as the registration map. Keep new registrations in the existing responsibility-specific methods rather than adding a second composition mechanism.
- Follow the existing MassTransit consumer registrations when adding handlers. Reuse the [Core command-handler layout](../TransportOrder.Core/Services/CommandHandler) and register the handler through the same host wiring as its neighbors.
- Preserve service lifetimes: the host mixes transient repositories, scoped services, singleton caches, and hosted services. Do not capture scoped dependencies in a singleton or duplicate a shared hosted-service instance under separate interfaces.
- Register background initialization and recurring work using the existing hosted-service patterns. Keep their implementation in the owning project rather than adding processing loops to startup code.
- AGV/action-group selection is configuration-driven in `RegisterAgvServices`. Preserve existing modes and fallback behavior when changing chooser registration; do not hardcode a replacement strategy for every deployment.
- Database setup uses `AddDbContextFactory<TransportOrderDbContext>` with the configured connection string and existing interceptors. Preserve this pattern and repository abstractions rather than creating long-lived database contexts in singleton services.

## Focused Validation

Run these commands from this directory; inspect failures for missing private-feed authentication before changing project configuration.

```bash
dotnet build TransportOrder.csproj --configuration Release
dotnet test ../TransportOrder.Test/TransportOrder.Test.csproj --configuration Release --filter "FullyQualifiedName~TransportOrderHostBuilderTests"
```

- The [host-builder test](../TransportOrder.Test/TransportOrderHostBuilderTests.cs) checks for captive dependencies without starting the complete service stack. Run it after DI or lifetime changes.
- For behavior changes, select the affected tests in [TransportOrder.Test](../TransportOrder.Test/TransportOrder.Test.csproj), using `--filter "FullyQualifiedName~<test-class-or-method>"`. Remove the filter for the full component suite when needed.
- Reuse neighboring MassTransit test fixtures, such as [NodeStationCommandHandlerTestFixture](../TransportOrder.Test/Services/CommandHandler/Node/NodeStationCommandHandlerTestFixture.cs), instead of starting RabbitMQ or Docker for a consumer unit test.
- For state changes, verify the current domain implementation rather than assuming historical diagrams are authoritative. Start with the [module-interaction documentation](../../../Doku/ModuleInteraction.md) for cross-service effects.