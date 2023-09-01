using MassTransit;

namespace TransportOrder.Messages.Events.Agv;

/// <summary>
/// AcceptAgv was rejected
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record AcceptAgvRejected(Guid CorrelationId) : CorrelatedBy<Guid>;