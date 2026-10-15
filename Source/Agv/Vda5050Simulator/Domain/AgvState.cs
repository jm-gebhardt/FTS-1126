namespace Gebhardt.Acc.Vda5050Simulator.Domain;

public enum ConnectionState
{
    Online,
    Offline,
    ConnectionBroken
}

public enum ActionStatus
{
    Waiting,
    Initializing,
    Running,
    Paused,
    Finished,
    Failed
}

public enum ErrorLevel
{
    Warning,
    Fatal
}

public sealed record ActionState(string ActionId, string ActionType, ActionStatus Status);

public sealed record NodeState(string NodeId, int SequenceId, bool Released);

public sealed record EdgeState(string EdgeId, int SequenceId, bool Released);

public sealed record ErrorReference(string Key, string Value);

public sealed record AgvError(string ErrorType, ErrorLevel Level, string Description, IReadOnlyList<ErrorReference> References);

public sealed record AgvPositionSnapshot(double X, double Y, double Theta, string MapId, bool PositionInitialized);

/// <summary>
/// Immutable view of the AGV state at one point in (virtual) time, ready to be published.
/// </summary>
public sealed record AgvStateSnapshot(
    long HeaderId,
    DateTimeOffset Timestamp,
    string OrderId,
    int OrderUpdateId,
    string LastNodeId,
    int LastNodeSequenceId,
    bool Driving,
    double BatteryCharge,
    bool Charging,
    IReadOnlyList<ActionState> ActionStates,
    IReadOnlyList<NodeState> NodeStates,
    IReadOnlyList<EdgeState> EdgeStates,
    IReadOnlyList<AgvError> Errors,
    AgvPositionSnapshot? Position);
