using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Statistics.Cache;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.Statistics.Test.Fixtures;

internal class GridPositionAttachedHandlerTestFixture<THandler, TMessage> : FixtureBase
    where THandler : class
    where TMessage : class
{
    private readonly Mock<IReadOnlyAgvScopedPositionCache<Guid>> _positionCacheMock = new();

    private readonly Mock<IAgvScopedPositionCache<TMessage>> _pendingCacheMock = new();
    private readonly Mock<ILogger<THandler>> _loggerMock = new();

    public Mock<IPublishEndpoint> PublishEndpointMock { get; } = new();

    public THandler Handler => Provider.GetRequiredService<THandler>();

    public GridPositionAttachedHandlerTestFixture()
    {
        Build();
    }

    protected override FixtureBase AddServices(IServiceCollection services)
    {
        services.AddSingleton(_positionCacheMock.Object);
        services.AddSingleton(_pendingCacheMock.Object);
        services.AddSingleton(PublishEndpointMock.Object);
        services.AddSingleton(_loggerMock.Object);

        services.AddTransient<THandler>();

        return base.AddServices(services);
    }
}