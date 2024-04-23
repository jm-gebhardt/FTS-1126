using Gebhardt.Acc.Common.Domain.Models.Graphs;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Graphs.Events;

/// <summary>
/// Event that is published when the (global) graph has changed.
/// </summary>
/// <param name="CorrelationId"></param>
/// <param name="Graph"></param>
/// <param name="Maps">Optionally you can already specify maps</param>
public record GraphChanged(Guid CorrelationId, Graph Graph, List<Map>? Maps = null) : CorrelatedBy<Guid>;