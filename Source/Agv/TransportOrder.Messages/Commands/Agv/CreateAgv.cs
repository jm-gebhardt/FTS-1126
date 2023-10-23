using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to create an agv.
/// </summary>
/// <param name="CorrelationId">The correlationId of the agv.</param>
/// <param name="AgvId">The id of the agv.</param>
/// <param name="AgvStatus">The agv status.</param>
public record CreateAgv(Guid CorrelationId, string AgvId, AgvStatus AgvStatus) : CorrelatedBy<Guid>;