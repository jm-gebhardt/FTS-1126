using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Microsoft.Extensions.Logging;
using Moq;
using TransportOrder.Domain.Repositories;
using TransportOrder.Domain.Services.Routing;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.Routing;

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