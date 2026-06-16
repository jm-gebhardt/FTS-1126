using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Graph;

/// <summary>
/// Context for a margin strategy.
/// </summary>
[PublicAPI]
public record MarginContext(GraphNode? Node, GraphEdge? Edge, GraphNode? FromNode, GraphNode? ToNode, AgvData AgvData, Vector2 DrivingDirection);