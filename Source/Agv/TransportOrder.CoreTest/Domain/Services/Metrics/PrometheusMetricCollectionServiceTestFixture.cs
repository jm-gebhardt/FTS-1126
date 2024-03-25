using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Metrics;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.Metrics;

public class PrometheusMetricCollectionServiceTestFixture
{
    private readonly Mock<IAgvDb> _agvDbMock = new();
    
    public PrometheusMetricCollectionService Service { get; }

    public PrometheusMetricCollectionServiceTestFixture()
    {
        Service = new PrometheusMetricCollectionService(_agvDbMock.Object);
    }

    public PrometheusMetricCollectionServiceTestFixture AddGraph(AgvGraph graph)
    {
        _agvDbMock.Setup(x => x.Graphs.GetAgvGraphAsync(It.IsAny<Guid>())).ReturnsAsync(graph);
        return this;
    }
}