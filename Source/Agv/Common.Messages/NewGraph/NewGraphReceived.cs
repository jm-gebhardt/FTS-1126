using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.NewGraph;

/// <summary>
/// Represents an event raised when a new graph has been received.
/// </summary>
/// <param name="CorrelationId">The identifier of this message.</param>
/// <param name="Graph">The graph that was received.</param>
public record NewGraphReceived(Guid CorrelationId, Graph Graph) : CorrelatedBy<Guid>;