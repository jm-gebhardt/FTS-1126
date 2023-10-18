using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Event that is raised when an agv was successfully set to idle.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
[PublicAPI]
public record SetAgvIdleSucceeded(Guid CorrelationId) : CorrelatedBy<Guid>;