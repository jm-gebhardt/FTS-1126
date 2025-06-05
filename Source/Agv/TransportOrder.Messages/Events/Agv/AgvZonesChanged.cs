namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents and event that is raised, when the zones of an agv change.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Zones">The zones of the agv.</param>
public record AgvZonesChanged(Guid CorrelationId, ICollection<string> Zones);