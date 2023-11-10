using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Implementations;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.Routing;

public class RouteServiceTestFixture
{
    private readonly Mock<IAgvGraphRepository> _agvGraphRepository;

    public RouteService RouteService { get; }

    public RouteServiceTestFixture()
    {
        _agvGraphRepository = new Mock<IAgvGraphRepository>();
        RouteService = new RouteService(_agvGraphRepository.Object, new AStarRouteFinder(), new Mock<ILogger<RouteService>>().Object);
    }

    public RouteServiceTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv, AgvGraph agvGraph)
    {
        _agvGraphRepository.Setup(x => x.ExistsAsync(agv.CorrelationId)).ReturnsAsync(true);
        _agvGraphRepository.Setup(x => x.GetAgvGraphAsync(agv.CorrelationId)).ReturnsAsync(agvGraph);

        return this;
    }
}