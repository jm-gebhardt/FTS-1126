using Gebhardt.Acc.Common.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to update system-issued AGV errors by adding and resolving specific errors.
/// </summary>
/// <param name="CorrelationId">The AGV correlation ID.</param>
/// <param name="ErrorsToAdd">The list of AGV errors to be added.</param>
/// <param name="ErrorsToResolve">The list of AGV errors to be resolved.</param>
/// <param name="Position">The AGV position associated with the errors.</param>
public record UpdateSystemAgvErrors(Guid CorrelationId, List<AgvError> ErrorsToAdd, List<AgvError> ErrorsToResolve, AgvPosition Position) : CorrelatedBy<Guid>;