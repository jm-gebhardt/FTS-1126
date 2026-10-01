using Gebhardt.Acc.Common.Domain.Models.Graphs;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Graphs.Events;

/// <summary>
/// Event that is published when the (global) graph has changed.
/// </summary>
/// <param name="CorrelationId">The correlation Id.</param>
/// <param name="Graph">The updated graph.</param>
/// <param name="Maps">Optionally, you can already specify maps</param>
public record GraphUpdated(Guid CorrelationId, Graph Graph, List<Map>? Maps = null) : CorrelatedBy<Guid>;