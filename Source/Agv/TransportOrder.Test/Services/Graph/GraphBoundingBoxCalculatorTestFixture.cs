using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

namespace Gebhardt.Acc.TransportOrder.Test.Services.Graph;

public class GraphBoundingBoxCalculatorTestFixture
{
    private List<IEdgePolygonProvider> _providers = [new DirectEdgePolygonProvider()];
    private Common.Domain.Models.Graphs.Graph _graph = new();

    public GraphBoundingBoxCalculator Calculator => new(_graph, _providers);

    public GraphBoundingBoxCalculatorTestFixture WithGraph(Common.Domain.Models.Graphs.Graph graph)
    {
        _graph = graph;
        return this;
    }

    public GraphBoundingBoxCalculatorTestFixture WithProviders(params IEdgePolygonProvider[] providers)
    {
        _providers = providers.ToList();
        return this;
    }
}