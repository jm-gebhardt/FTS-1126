using Gebhardt.Acc.Common.Memory;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Statistics.Extensions;
using Gebhardt.Acc.Statistics.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.Statistics.Test.Fixtures;

internal class AgvPositionReceivedHandlerTestFixture : MockDbConsumerTestFixture<AgvPositionReceivedHandler>
{
    private readonly Mock<IMessageFilter<AgvPositionReceived>> _agvPositionFilterMock = new();

    protected override MassTransitConsumerTestFixture<AgvPositionReceivedHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _agvPositionFilterMock.Object)
            .AddSingleton(typeof(ICache<,>), typeof(Cache<,>))
            .AddAgvScopedCache<Guid>(1000, false);
        
        return this;
    }

    public AgvPositionReceivedHandlerTestFixture ShouldActReturns(bool shouldAct)
    {
        _agvPositionFilterMock.Setup(m => m.ShouldActAsync(It.IsAny<AgvPositionReceived>())).ReturnsAsync(shouldAct);
        return this;
    }
}