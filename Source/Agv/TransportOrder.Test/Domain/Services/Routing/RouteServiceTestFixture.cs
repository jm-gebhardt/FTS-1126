using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Routing.Implementations;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.Routing;

public class RouteServiceTestFixture
{
    private readonly Mock<IGraphRepository> _agvGraphRepository;

    public RouteService RouteService { get; }

    public RouteServiceTestFixture()
    {
        _agvGraphRepository = new Mock<IGraphRepository>();
        RouteService = new RouteService(_agvGraphRepository.Object, new AStarRouteFinder(), new Mock<ILogger<RouteService>>().Object);
    }

    public RouteServiceTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv, AgvGraph agvGraph)
    {
        _agvGraphRepository.Setup(x => x.ExistsAsync(agv.CorrelationId)).ReturnsAsync(true);
        _agvGraphRepository.Setup(x => x.GetAgvGraphAsync(agv.CorrelationId)).ReturnsAsync(agvGraph);

        return this;
    }
}