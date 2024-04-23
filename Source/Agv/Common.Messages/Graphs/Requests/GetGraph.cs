using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Graphs.Requests;

/// <summary>
/// Request to get the current (global) graph.
/// </summary>
/// <param name="CorrelationId">Any correlation id for tracking.</param>
public record GetGraph(Guid CorrelationId) : CorrelatedBy<Guid>;