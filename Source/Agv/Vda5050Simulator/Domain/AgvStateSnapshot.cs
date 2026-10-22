namespace Gebhardt.Acc.Vda5050Simulator.Domain;

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
    IReadOnlyList<Error> Errors,
    AgvPositionSnapshot? Position,
    Gebhardt.Acc.Common.Domain.Models.AgvOperatingMode OperatingMode);