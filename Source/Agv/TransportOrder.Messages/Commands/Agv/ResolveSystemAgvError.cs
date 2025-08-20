using Gebhardt.Acc.Common.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to resolve multiple system-issued AGV errors.
/// </summary>
/// <param name="CorrelationId">The AGV correlation ID.</param>
/// <param name="Errors"> The AGV errors to resolve.</param>
public record ResolveSystemAgvError(Guid CorrelationId, List<AgvError> Errors) : CorrelatedBy<Guid>;