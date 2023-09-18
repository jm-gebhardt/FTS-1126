using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// AcceptAgv was rejected
/// </summary>
[PublicAPI]
public record AcceptAgvRejected(Guid CorrelationId) : CorrelatedBy<Guid>;