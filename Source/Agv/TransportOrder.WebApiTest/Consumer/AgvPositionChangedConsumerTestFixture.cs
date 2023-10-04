using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

public class AgvPositionChangedConsumerTestFixture : ConsumerHubTestFixture<AgvPositionChangedConsumer, IAgvPublisher>
{
    public AgvPositionChangedConsumerTestFixture WithNodeReserved(string nodeId, bool reserve = true)
    {
        NodeStateRegistryMock.Setup(x => x.IsNodeReservedAsync(nodeId)).Returns(Task.FromResult(reserve));
        return this;
    }

    public AgvPositionChangedConsumerTestFixture WithAgvRoute(Guid agvCorrelationId, string lastNodeId, AgvRouteReadModel? agvRoute)
    {
        AgvRouteServiceMock.Setup(x => x.GetAgvRouteAsync(agvCorrelationId, lastNodeId)).Returns(Task.FromResult(agvRoute));
        return this;
    }
}