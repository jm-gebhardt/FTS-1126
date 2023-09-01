using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using MassTransit;

namespace TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the current position of an AGV has changed
/// </summary>
/// <param name="NewAgvPosition">New AGV position</param>
/// <param name="PreviousAgvPosition">Previous AGV position</param>
/// <param name="AgvId">Identifier of the AGV</param>
/// <param name="CorrelationId">The correlationId of the new agv position</param>
[JetBrains.Annotations.PublicAPI]
public record AgvPositionChanged(AgvPosition NewAgvPosition, AgvPosition? PreviousAgvPosition, string AgvId, Guid CorrelationId) : CorrelatedBy<Guid>;