using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.ActionGroups;

/// <summary>
/// Describes the route assigned to an AGV, split into a base segment (directly executable) and a horizon segment (planned, but not fixed).
/// </summary>
/// <param name="BaseNodes">Base nodes.</param>
/// <param name="BaseEdges">Base edges.</param>
/// <param name="HorizonNodes">Horizon nodes.</param>
/// <param name="HorizonEdges">Horizon edges.</param>
[PublicAPI]
public record RoutePlan(IReadOnlyList<GraphNode> BaseNodes, IReadOnlyList<GraphEdge> BaseEdges, IReadOnlyList<GraphNode> HorizonNodes, IReadOnlyList<GraphEdge> HorizonEdges);