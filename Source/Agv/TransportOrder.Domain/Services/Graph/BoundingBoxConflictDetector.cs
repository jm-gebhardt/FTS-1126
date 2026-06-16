using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using CommonGraph = Gebhardt.Acc.Common.Domain.Models.Graphs.Graph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

/// <summary>
/// Detects pairs of graph resources whose worst-case bounding boxes geometrically intersect.
/// </summary>
[PublicAPI]
public class BoundingBoxConflictDetector
{
    private readonly IReadOnlyList<ResourceBoundingBox> _boundingBoxes;
    private readonly HashSet<(Guid NodeId, Guid EdgeId)> _adjacency;
    private readonly Dictionary<Guid, (Guid FromNodeId, Guid ToNodeId)> _edgeEndpoints;

    /// <inheritdoc cref="BoundingBoxConflictDetector"/>
    /// <param name="boundingBoxes">One bounding box per resource, pre-selected for the worst-case AGV type.</param>
    /// <param name="graph">The graph used to determine resource adjacency.</param>
    public BoundingBoxConflictDetector(IReadOnlyList<ResourceBoundingBox> boundingBoxes, CommonGraph graph)
    {
        _boundingBoxes = boundingBoxes;
        _adjacency = [];
        _edgeEndpoints = [];

        foreach (var edge in graph.Edges)
        {
            _adjacency.Add((edge.FromNodeId, edge.Id));
            _adjacency.Add((edge.ToNodeId, edge.Id));
            _edgeEndpoints[edge.Id] = (edge.FromNodeId, edge.ToNodeId);
        }
    }

    private bool IsAdjacent(ResourceBoundingBox a, ResourceBoundingBox b)
    {
        if (a.ResourceType == ResourceType.Edge && b.ResourceType == ResourceType.Edge)
        {
            return ShareEndpoint(a.ResourceId, b.ResourceId);
        }

        if (a.ResourceType == ResourceType.Edge)
        {
            (a, b) = (b, a);
        }

        return a.ResourceType == ResourceType.Node &&
               b.ResourceType == ResourceType.Edge &&
               _adjacency.Contains((a.ResourceId, b.ResourceId));
    }

    private bool ShareEndpoint(Guid edgeAId, Guid edgeBId)
    {
        if (!_edgeEndpoints.TryGetValue(edgeAId, out var a) ||
            !_edgeEndpoints.TryGetValue(edgeBId, out var b))
        {
            return false;
        }

        return a.FromNodeId == b.FromNodeId || a.FromNodeId == b.ToNodeId ||
               a.ToNodeId == b.FromNodeId || a.ToNodeId == b.ToNodeId;
    }

    private bool AreConflicting(ResourceBoundingBox a, ResourceBoundingBox b)
    {
        return !IsAdjacent(a, b) && a.Polygon.IsIntersecting(b.Polygon);
    }

    /// <summary>
    /// Returns all conflicting pairs that involve the resource with the given ID.
    /// </summary>
    public IReadOnlyList<BoundingBoxConflict> DetectFor(Guid resourceId)
    {
        var target = _boundingBoxes.FirstOrDefault(b => b.ResourceId == resourceId);
        if (target is null)
        {
            return [];
        }

        return _boundingBoxes
            .Where(other => other.ResourceId != resourceId && AreConflicting(target, other))
            .Select(other => new BoundingBoxConflict(target, other))
            .ToList();
    }

    /// <summary>
    /// Returns all conflicting resource pairs.
    /// </summary>
    public IReadOnlyList<BoundingBoxConflict> Detect()
    {
        var conflicts = new List<BoundingBoxConflict>();

        for (int i = 0; i < _boundingBoxes.Count; i++)
        {
            for (int j = i + 1; j < _boundingBoxes.Count; j++)
            {
                var a = _boundingBoxes[i];
                var b = _boundingBoxes[j];

                if (AreConflicting(a, b))
                {
                    conflicts.Add(new BoundingBoxConflict(a, b));
                }
            }
        }

        return conflicts;
    }
}