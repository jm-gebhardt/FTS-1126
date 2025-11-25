using Gebhardt.Acc.Common.Memory;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.Statistics.Test.Fixtures;

internal class AgvPositionReceivedHandlerTestFixture : MockDbConsumerTestFixture<AgvPositionReceivedHandler>
{
    private readonly Mock<IMessageFilter<AgvPositionReceived>> _agvPositionFilterMock = new();

    private readonly ICache<Guid, List<(GridCell GridCell, DateTime EntryTime)>> _cache = new Cache<Guid, List<(GridCell GridCell, DateTime EntryTime)>>();

    protected override MassTransitConsumerTestFixture<AgvPositionReceivedHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection
            .AddTransient(_ => _agvPositionFilterMock.Object)
            .AddSingleton(_cache);

        return this;
    }

    public AgvPositionReceivedHandlerTestFixture ShouldActReturns(bool shouldAct)
    {
        _agvPositionFilterMock.Setup(m => m.ShouldActAsync(It.IsAny<AgvPositionReceived>())).ReturnsAsync(shouldAct);
        return this;
    }

    public AgvPositionReceivedHandlerTestFixture WithLastEntryTime(Guid agvCorrelationId, GridCell gridCell, DateTime entryTime)
    {
        _cache.AddOrUpdate(agvCorrelationId, [(gridCell, entryTime)]);
        return this;
    }
}