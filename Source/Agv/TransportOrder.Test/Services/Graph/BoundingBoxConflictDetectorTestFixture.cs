using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using RoutingGraph = Gebhardt.Acc.Common.Domain.Models.NewGraph.Graph;

namespace Gebhardt.Acc.TransportOrder.Test.Services.Graph;

public class BoundingBoxConflictDetectorTestFixture
{
    private readonly List<ResourceBoundingBox> _boxes = [];
    private RoutingGraph _graph = RoutingGraphGenerator.GenerateGraph();

    public BoundingBoxConflictDetector Detector => new(_boxes, _graph);

    public BoundingBoxConflictDetectorTestFixture WithBoxes(params ResourceBoundingBox[] boxes)
    {
        _boxes.AddRange(boxes);
        return this;
    }

    public BoundingBoxConflictDetectorTestFixture WithGraph(RoutingGraph graph)
    {
        _graph = graph;
        return this;
    }
}