using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services;

public class OrderEventHandlerTestFixture : MassTransitConsumerTestFixture<OrderEventHandler>
{
    private readonly Mock<IAgvRepository> _agvRepository = new();

    public OrderEventHandlerTestFixture()
    {
        AddServices(sc =>
        {
            sc.AddSingleton<IOrderActionStatusCacheProvider, OrderActionStatusCache>();
            sc.AddTransient(_ => _agvRepository.Object);
        });
        Build();
    }

    public OrderEventHandlerTestFixture AddAgv(Agv agv)
    {
        _agvRepository.Setup(x => x.GetAgvByCurrentOrderAsync(It.IsAny<Guid>())).ReturnsAsync(agv);
        return this;
    }
}