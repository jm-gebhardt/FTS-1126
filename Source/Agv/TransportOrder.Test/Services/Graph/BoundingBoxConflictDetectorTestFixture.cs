using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

namespace Gebhardt.Acc.TransportOrder.Test.Services.Graph;

public class BoundingBoxConflictDetectorTestFixture
{
    private readonly List<ResourceBoundingBox> _boxes = [];
    private Common.Domain.Models.Graphs.Graph _graph = new();

    public BoundingBoxConflictDetector Detector => new(_boxes, _graph);

    public BoundingBoxConflictDetectorTestFixture WithBoxes(params ResourceBoundingBox[] boxes)
    {
        _boxes.AddRange(boxes);
        return this;
    }

    public BoundingBoxConflictDetectorTestFixture WithGraph(Common.Domain.Models.Graphs.Graph graph)
    {
        _graph = graph;
        return this;
    }
}