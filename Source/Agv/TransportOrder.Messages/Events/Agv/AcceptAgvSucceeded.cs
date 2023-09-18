using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// AcceptAgv was successful
/// </summary>
[PublicAPI]
public record AcceptAgvSucceeded(Guid CorrelationId) : CorrelatedBy<Guid>;