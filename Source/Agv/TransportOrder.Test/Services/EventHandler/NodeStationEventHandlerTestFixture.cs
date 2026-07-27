using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

public class NodeStationEventHandlerTestFixture: MassTransitConsumerTestFixture<NodeStationEventHandler>
{
    private readonly Mock<IAgvDb> _agvDbMock = new();

    public NodeStationEventHandlerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _agvDbMock.Object);
        });
        Build();
    }

    public NodeStationEventHandlerTestFixture WithChargingAgvs(ICollection<Agv> agvs)
    {
        _agvDbMock.Setup(x => x.Agvs.GetAgvsOnChargerReachableByAgvWithAtLeastMinChargeUpLevel(It.IsAny<Guid?>())).ReturnsAsync(agvs);
        return this;
    }
}