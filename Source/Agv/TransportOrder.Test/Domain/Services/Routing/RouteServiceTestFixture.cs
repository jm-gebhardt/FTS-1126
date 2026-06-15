using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Routing.Implementations;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.Routing;

public class RouteServiceTestFixture
{
    private readonly Mock<IGraphRepository> _agvGraphRepository = new();
    private readonly Mock<IQueueRepository> _queueRepository = new();

    public Mock<ILoggingService> LoggingService { get; } = new();

    public RouteService RouteService { get; }

    public RouteServiceTestFixture()
    {
        RouteService = new RouteService(_agvGraphRepository.Object, new AStarRouteFinder(), new Mock<ILogger<RouteService>>().Object, _queueRepository.Object, LoggingService.Object);
    }

    public RouteServiceTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv, AgvGraph agvGraph)
    {
        _agvGraphRepository.Setup(x => x.ExistsAsync(agv.CorrelationId)).ReturnsAsync(true);
        _agvGraphRepository.Setup(x => x.GetAgvGraphAsync(agv.CorrelationId)).ReturnsAsync(agvGraph);

        return this;
    }

    public RouteServiceTestFixture WithAgvs(ICollection<TransportOrder.Domain.Models.Agv.Agv> agvs, ICollection<AgvGraph> agvGraphs)
    {
        foreach (var (agv, agvGraph) in agvs.Zip(agvGraphs))
        {
            _agvGraphRepository.Setup(x => x.ExistsAsync(agv.CorrelationId)).ReturnsAsync(true);
            _agvGraphRepository.Setup(x => x.GetAgvGraphAsync(agv.CorrelationId)).ReturnsAsync(agvGraph);
        }

        return this;
    }

    public RouteServiceTestFixture WithBlockedQueueNodes(string[] nodes)
    {
        _queueRepository.Setup(x => x.GetBlockedQueueNodes()).ReturnsAsync(nodes.Select(n => n.ToGuid()).ToList);
        return this;
    }
}