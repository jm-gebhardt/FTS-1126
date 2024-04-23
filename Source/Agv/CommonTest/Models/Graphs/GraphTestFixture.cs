using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;

namespace Gebhardt.Acc.CommonTest.Models.Graphs;

internal class GraphTestFixture
{
    private GraphEdge[] _graphEdges = Array.Empty<GraphEdge>();

    private GraphEdge[] _agvGraphEdges = Array.Empty<GraphEdge>();

    private GraphNode[] _graphNodes = Array.Empty<GraphNode>();

    private GraphNode[] _agvGraphNodes = Array.Empty<GraphNode>();

    public Graph Graph { get; private set; } = null!;

    public AgvGraph AgvGraph { get; private set; } = null!;

    public GraphTestFixture WithGraphEdges(params GraphEdge[] graphEdges)
    {
        _graphEdges = graphEdges;
        return this;
    }

    public GraphTestFixture WithGraphNodes(params GraphNode[] graphNodes)
    {
        _graphNodes = graphNodes;
        return this;
    }

    public GraphTestFixture WithAgvGraphEdges(params GraphEdge[] graphEdges)
    {
        _agvGraphEdges = graphEdges;
        return this;
    }

    public GraphTestFixture WithAgvGraphNodes(params GraphNode[] graphNodes)
    {
        _agvGraphNodes = graphNodes;
        return this;
    }

    public GraphTestFixture Build()
    {
        Graph = new Graph
        {
            Edges = _graphEdges.ToList(),
            Nodes = _graphNodes.ToList()
        };

        AgvGraph = AgvGraphGenerator.Generate(_agvGraphNodes, _agvGraphEdges);

        return this;
    }
}