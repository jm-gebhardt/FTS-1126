using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using JetBrains.Annotations;
using DomainNode = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.Node;
using DomainEdge = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.Edge;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;

/// <summary>
/// Extension methods for <see cref="DomainEdge"/> collections.
/// </summary>
[PublicAPI]
public static class EdgeExtensions
{
    /// <summary>
    /// Ensures every domain edge has a valid trajectory. Edges without an explicit trajectory
    /// get a straight-line fallback derived from their start/end node positions.
    /// </summary>
    public static void ApplyFallbackTrajectories(this List<DomainEdge> edges, List<DomainNode> nodes)
    {
        Dictionary<string, DomainNode> nodesByNodeId = nodes.ToDictionary(n => n.NodeId, n => n);

        foreach (var edge in edges)
        {
            if (edge.AgvTrajectory is { ControlPoints.Count: > 0 })
            {
                continue;
            }

            var startNode = nodesByNodeId[edge.StartNodeId];
            var endNode = nodesByNodeId[edge.EndNodeId];

            edge.AgvTrajectory = startNode.Position.CreateLine(endNode.Position);
        }
    }
}

