namespace Gebhardt.Acc.Vda5050Simulator.Domain;

public enum BlockingType
{
    None,
    Soft,
    Hard
}

public sealed record ActionParameter(string Key, string Value);

public sealed record AgvAction(
    string ActionId,
    string ActionType,
    BlockingType BlockingType,
    string? Description,
    IReadOnlyList<ActionParameter> Parameters);

public sealed record NodePosition(double X, double Y, double? Theta, double? AllowedDeviationXY, string MapId);

public sealed record ControlPoint(double X, double Y, double Weight);

public sealed record NurbsTrajectory(int Degree, IReadOnlyList<double> KnotVector, IReadOnlyList<ControlPoint> ControlPoints);

public sealed record OrderNode(
    string NodeId,
    int SequenceId,
    bool Released,
    NodePosition Position,
    IReadOnlyList<AgvAction> Actions);

public sealed record OrderEdge(
    string EdgeId,
    int SequenceId,
    bool Released,
    string StartNodeId,
    string EndNodeId,
    double? MaxSpeed,
    NurbsTrajectory? Trajectory,
    IReadOnlyList<AgvAction> Actions);

public sealed record Order(
    string OrderId,
    int OrderUpdateId,
    IReadOnlyList<OrderNode> Nodes,
    IReadOnlyList<OrderEdge> Edges);

public sealed record InstantAction(AgvAction Action);
