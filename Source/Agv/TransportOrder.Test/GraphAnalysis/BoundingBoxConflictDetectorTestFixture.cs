using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

namespace Gebhardt.Acc.TransportOrder.Test.GraphAnalysis;

public class BoundingBoxConflictDetectorTestFixture
{
    private readonly List<ResourceBoundingBox> _boxes = [];
    private Graph _graph = new();

    public BoundingBoxConflictDetector Detector => new(_boxes, _graph);

    public BoundingBoxConflictDetectorTestFixture WithBoxes(params ResourceBoundingBox[] boxes)
    {
        _boxes.AddRange(boxes);
        return this;
    }

    public BoundingBoxConflictDetectorTestFixture WithGraph(Graph graph)
    {
        _graph = graph;
        return this;
    }
}