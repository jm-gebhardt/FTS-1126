namespace Gebhardt.Acc.Vda5050Simulator.Messaging.Dtos;

public sealed class ActionParameterDto
{
    public string? Key { get; init; }

    public System.Text.Json.JsonElement Value { get; init; }
}

public sealed class ActionDto
{
    public string? ActionId { get; init; }
    public string? ActionType { get; init; }
    public string? ActionDescription { get; init; }
    public string? BlockingType { get; init; }
    public List<ActionParameterDto>? ActionParameters { get; init; }
}

public sealed class NodePositionDto
{
    public double X { get; init; }
    public double Y { get; init; }
    public double? Theta { get; init; }
    public double? AllowedDeviationXY { get; init; }
    public string? MapId { get; init; }
}

public sealed class ControlPointDto
{
    public double X { get; init; }
    public double Y { get; init; }
    public double? Weight { get; init; }
}

public sealed class TrajectoryDto
{
    public int Degree { get; init; }
    public List<double>? KnotVector { get; init; }
    public List<ControlPointDto>? ControlPoints { get; init; }
}

public sealed class NodeDto
{
    public string? NodeId { get; init; }
    public int SequenceId { get; init; }
    public bool Released { get; init; }
    public NodePositionDto? NodePosition { get; init; }
    public List<ActionDto>? Actions { get; init; }
}

public sealed class EdgeDto
{
    public string? EdgeId { get; init; }
    public int SequenceId { get; init; }
    public bool Released { get; init; }
    public string? StartNodeId { get; init; }
    public string? EndNodeId { get; init; }
    public double? MaxSpeed { get; init; }
    public TrajectoryDto? Trajectory { get; init; }
    public List<ActionDto>? Actions { get; init; }
}

public sealed class OrderDto
{
    public string? OrderId { get; init; }
    public int? OrderUpdateId { get; init; }
    public List<NodeDto>? Nodes { get; init; }
    public List<EdgeDto>? Edges { get; init; }
}

public sealed class InstantActionsDto
{
    public List<ActionDto>? Actions { get; init; }
}
