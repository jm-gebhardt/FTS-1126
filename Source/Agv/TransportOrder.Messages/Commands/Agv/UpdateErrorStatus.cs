using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to update agv errors.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="Errors">The errors of the agv.</param>
public record UpdateErrorStatus(Guid CorrelationId, List<AgvError> Errors) : CorrelatedBy<Guid>;