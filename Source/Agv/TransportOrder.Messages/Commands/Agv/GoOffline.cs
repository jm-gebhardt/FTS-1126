using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to set an agv offline.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
///  <param name="OfflineTokenId">The offline token id.</param>
public record GoOffline(Guid CorrelationId, Guid OfflineTokenId) : CorrelatedBy<Guid>;