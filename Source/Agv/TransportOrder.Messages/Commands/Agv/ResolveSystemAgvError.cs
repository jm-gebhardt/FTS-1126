using Gebhardt.Acc.Common.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to resolve multiple system-issued AGV errors.
/// </summary>
/// <param name="CorrelationId">The AGV correlation ID.</param>
/// <param name="Error"> The AGV error to resolve.</param>
/// <param name="Position">The AGV position where the errors occurred.</param>
public record ResolveSystemAgvError(Guid CorrelationId, AgvError Error, AgvPosition Position) : CorrelatedBy<Guid>;