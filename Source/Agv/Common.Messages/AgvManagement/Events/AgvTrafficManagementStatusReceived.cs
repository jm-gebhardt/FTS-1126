namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting the current TrafficManagementStatus
/// </summary>
public record AgvTrafficManagementStatusReceived
{
    public required Guid AgvId { get; init; }

    public required bool IsPositionInitialized { get; init; }
    
    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Theta { get; init; }

    public required string? LastLifNodeId { get; init; }

    public required string? MapId { get; init; }

    public required bool IsLoaded { get; init; }

    public required bool IsAutomatic { get; init; }

    public required bool HasFatalError { get; init; }

    public required bool IsIdle { get; init; }
}