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

public sealed record Error(string ErrorType, ErrorLevel Level, string? Description, IReadOnlyList<ErrorReference>? ErrorReferences);

public sealed record AgvPositionSnapshot(double X, double Y, double Theta, string MapId, bool PositionInitialized);