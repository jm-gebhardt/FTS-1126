using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using RoutingGraph = Gebhardt.Acc.Common.Domain.Models.NewGraph.Graph;

namespace Gebhardt.Acc.TransportOrder.Test.Services.Graph;

public class GraphBoundingBoxCalculatorTestFixture
{
    public const double AgvLength = 4.0;
    public const double AgvWidth = 2.0;

    private List<IEdgePolygonProvider> _providers = [new DirectEdgePolygonProvider()];
    private RoutingGraph _graph = RoutingGraphGenerator.GenerateGraph();

    public GraphBoundingBoxCalculator Calculator => new(_graph, _providers);

    public GraphBoundingBoxCalculatorTestFixture WithGraph(RoutingGraph graph)
    {
        _graph = graph;
        return this;
    }

    public GraphBoundingBoxCalculatorTestFixture WithProviderDirection(double direction)
    {
        _providers = [new EdgePolygonProviderMock(direction)];
        return this;
    }

    private class EdgePolygonProviderMock(double direction) : IEdgePolygonProvider
    {
        public bool Supports(Trajectory? trajectory)
        {
            return true;
        }

        public Polygon GetPolygon(RoutingEdge edge, Node startNode, Node endNode, IAgvType agvType, IReadOnlyList<IMarginStrategy> margins)
        {
            return new Polygon([new PolygonPoint(0, 0), new PolygonPoint(1, 0), new PolygonPoint(0, 1)]);
        }

        public double GetDirectionAtEndpoint(RoutingEdge edge, Node startNode, Node endNode)
        {
            return direction;
        }
    }
}