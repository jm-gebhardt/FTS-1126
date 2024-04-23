namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a request to get action group statistics.
/// </summary>
/// <param name="SourceNodeId">The source node id of the action group (optional).</param>
/// <param name="TargetNodeId">The target node id of the action group (optional).</param>
public record GetActionGroupStatistics(string? SourceNodeId, string? TargetNodeId);