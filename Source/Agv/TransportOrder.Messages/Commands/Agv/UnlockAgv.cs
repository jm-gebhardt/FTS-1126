using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Represents the UnlockAgv request
/// </summary>
[PublicAPI]
public record UnlockAgv(Guid CorrelationId) : CorrelatedBy<Guid>;