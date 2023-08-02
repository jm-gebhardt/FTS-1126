using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Routing;

public class RouteServiceTestFixture
{
    private readonly Mock<IAgvGraphRegistry> _agvGraphRegistry;

    public RouteService RouteService { get; }

    public RouteServiceTestFixture()
    {
        _agvGraphRegistry = new Mock<IAgvGraphRegistry>();
        RouteService = new RouteService(_agvGraphRegistry.Object, new Mock<ILogger<RouteService>>().Object);
    }

    public RouteServiceTestFixture WithAgv(IAgv agv, AgvGraph agvGraph)
    {
        _agvGraphRegistry.Setup(x => x.ExistsAsync(agv.CorrelationId)).ReturnsAsync(true);
        _agvGraphRegistry.Setup(x => x.GetAgvGraphAsync(agv.CorrelationId)).ReturnsAsync(agvGraph);

        return this;
    }
}