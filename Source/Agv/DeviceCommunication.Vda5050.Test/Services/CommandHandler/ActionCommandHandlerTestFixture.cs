using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

public class ActionCommandHandlerTestFixture: MassTransitConsumerTestFixture<ActionCommandHandler>
{
    private readonly Mock<IOrderBuilder> _orderBuilder = new();
    private readonly Mock<IAgvRepository> _agvRepository = new();

    public ActionCommandHandlerTestFixture()
    {
        _agvRepository.Setup(x => x.AddOrUpdateAsync(It.IsAny<Agv>())).Verifiable();
        AddServices(sc =>
        {
            sc.AddSingleton(_ => _orderBuilder.Object);
            sc.AddTransient(_ => _agvRepository.Object);
        });
        Build();
    }

    public ActionCommandHandlerTestFixture AddOrder(Order order)
    {
        _orderBuilder.Setup(x => x.Build(It.IsAny<Guid>(), It.IsAny<AgvAction>())).ReturnsAsync(order);
        return this;
    }

    public ActionCommandHandlerTestFixture AddAgv(Agv agv)
    {
        _agvRepository.Setup(x => x.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync(agv);
        return this;
    }

    public void VerifyAgvUpdated()
    {
        _agvRepository.Verify(x => x.AddOrUpdateAsync(It.IsAny<Agv>()), Times.Once);
    }
}