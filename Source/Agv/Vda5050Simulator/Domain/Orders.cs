using Gebhardt.Acc.Common.Domain.Models.NewGraph;

namespace Gebhardt.Acc.Vda5050Simulator.Domain;

public sealed record NodePosition(double X, double Y, double? Theta, double? AllowedDeviationXy, string MapId);

public sealed record OrderNode(string NodeId, int SequenceId, bool Released, NodePosition Position, IReadOnlyList<AgvAction> Actions);

public sealed record OrderEdge(string EdgeId, int SequenceId, bool Released, string StartNodeId, string EndNodeId, double? MaxSpeed, Trajectory? Trajectory, IReadOnlyList<AgvAction> Actions);

public sealed record Order(string OrderId, int OrderUpdateId, IReadOnlyList<OrderNode> Nodes, IReadOnlyList<OrderEdge> Edges);

public sealed record InstantAction(AgvAction Action);
