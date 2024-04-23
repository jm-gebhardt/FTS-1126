using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Statistics.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.Statistics.Test.Fixtures;

internal class PositionEventHandlerTestFixture : MockDbConsumerTestFixture<PositionEventHandler>
{
    private readonly Mock<IMessageFilter<AgvPositionReceived>> _agvPositionFilterMock = new();

    protected override MassTransitConsumerTestFixture<PositionEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _agvPositionFilterMock.Object);
        return this;
    }

    public PositionEventHandlerTestFixture ShouldActReturns(bool shouldAct)
    {
        _agvPositionFilterMock.Setup(m => m.ShouldActAsync(It.IsAny<AgvPositionReceived>())).ReturnsAsync(shouldAct);
        return this;
    }
}