using Gebhardt.Acc.Common.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to add multiple system-issued AGV errors.
/// </summary>
/// <param name="CorrelationId">The AGV correlation ID.</param>
/// <param name="Errors">The list of system-generated errors.</param>
/// <param name="Position">The AGV position where the errors were detected.</param>
public record AddSystemAgvErrors(Guid CorrelationId, List<AgvError> Errors, AgvPosition Position) : CorrelatedBy<Guid>;