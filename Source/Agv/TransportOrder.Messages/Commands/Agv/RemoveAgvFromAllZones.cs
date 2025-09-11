using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a request to remove an agv from all zones.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="User">The user who removed the agv from all zones.</param>
public record RemoveAgvFromAllZones(Guid CorrelationId, string User) : CorrelatedBy<Guid>;