using MassTransit;

namespace TransportOrder.Messages.Events.Agv;

/// <summary>
/// AcceptAgv was successful
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record AcceptAgvSucceeded(Guid CorrelationId) : CorrelatedBy<Guid>;