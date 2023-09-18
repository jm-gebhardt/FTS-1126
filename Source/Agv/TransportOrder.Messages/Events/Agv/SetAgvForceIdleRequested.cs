using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Provides an event that is raised when the WebApi requests an agv to be set to idle by force.
/// </summary>
/// <param name="CorrelationId">The id of the agv to set to idle.</param>
public record SetAgvForceIdleRequested(Guid CorrelationId) : CorrelatedBy<Guid>;