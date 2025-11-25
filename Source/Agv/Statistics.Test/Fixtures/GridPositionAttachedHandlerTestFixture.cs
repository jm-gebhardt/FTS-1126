using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.Statistics.Test.Fixtures;

internal class GridPositionAttachedHandlerTestFixture<THandler, TMessage> : FixtureBase
    where THandler : class
    where TMessage : class, IMessageCreatedDate
{
    private readonly Mock<ILogger<THandler>> _loggerMock = new();
    private readonly Mock<IStatisticsDb> _statisticsDbMock = new();
    private readonly Mock<IGridCellRepository> _gridCellRepositoryMock = new();

    public Mock<IPublishEndpoint> PublishEndpointMock { get; } = new();

    public THandler Handler => Provider.GetRequiredService<THandler>();

    public GridPositionAttachedHandlerTestFixture()
    {
        _statisticsDbMock.Setup(s => s.GridCell).Returns(_gridCellRepositoryMock.Object);
    }

    protected override FixtureBase AddServices(IServiceCollection services)
    {
        services.AddSingleton(_statisticsDbMock.Object);
        services.AddSingleton(PublishEndpointMock.Object);
        services.AddSingleton(_loggerMock.Object);

        services.AddTransient<THandler>();

        return base.AddServices(services);
    }

    public new GridPositionAttachedHandlerTestFixture<THandler, TMessage> Build()
    {
        base.Build();
        return this;
    }
}