using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

namespace Gebhardt.Acc.TransportOrder.Test.GraphAnalysis;

public class GraphBoundingBoxCalculatorTestFixture
{
    private List<IEdgePolygonProvider> _providers = [new DirectEdgePolygonProvider()];
    private Graph _graph = new();

    public GraphBoundingBoxCalculator Calculator => new(_graph, _providers);

    public GraphBoundingBoxCalculatorTestFixture WithGraph(Graph graph)
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