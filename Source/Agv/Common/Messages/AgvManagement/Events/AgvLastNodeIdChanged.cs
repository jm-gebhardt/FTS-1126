using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting that the last node id of an AGV has changed
/// </summary>
/// <param name="CorrelationId"></param>
/// <param name="OldLastNodeId"></param>
/// <param name="NewLastNodeId"></param>
public record AgvLastNodeIdChanged(Guid CorrelationId, string? OldLastNodeId, string? NewLastNodeId): CorrelatedBy<Guid>;