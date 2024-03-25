using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Services.Filters;
using Gebhardt.StoreWare.Agv.Statistics.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.Statistics.Test.Fixtures;

internal class PositionEventHandlerTestFixture : MockDbConsumerTestFixture<PositionEventHandler>
{
    private readonly Mock<IMessageFilter<AgvPositionReceived>> _agvPositionFilterMock = new();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        base.ConfigureServices(collection);
        collection.AddTransient(_ => _agvPositionFilterMock.Object);
    }

    public PositionEventHandlerTestFixture ShouldActReturns(bool shouldAct)
    {
        _agvPositionFilterMock.Setup(m => m.ShouldActAsync(It.IsAny<AgvPositionReceived>())).ReturnsAsync(shouldAct);
        return this;
    }
}