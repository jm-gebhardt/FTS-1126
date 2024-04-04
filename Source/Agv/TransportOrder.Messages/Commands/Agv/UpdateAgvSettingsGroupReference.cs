using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to trigger updating the reference to the agv settings group an agv.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
public record UpdateAgvSettingsGroupReference(Guid CorrelationId) : CorrelatedBy<Guid>;