using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Graphs.Events;

/// <summary>
/// Event that is published when there is no (global) and it is needed.
/// </summary>
/// <param name="CorrelationId"></param>
public record GraphMissing(Guid CorrelationId) : CorrelatedBy<Guid>;