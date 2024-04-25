using Gebhardt.Acc.Common.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to update agv errors.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="Errors">The errors of the agv.</param>
/// <param name="Position">The position of the agv when the error status was received.</param>
public record UpdateErrorStatus(Guid CorrelationId, List<AgvError> Errors, AgvPosition Position) : CorrelatedBy<Guid>;