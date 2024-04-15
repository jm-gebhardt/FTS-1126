using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Graphs;

/// <summary>
/// Command to update the (global) graph.
/// </summary>
/// <param name="CorrelationId"></param>
/// <param name="Graph">The new (global) Graph.</param>
/// <param name="Maps">Optionally you can already specify maps.</param>
public record UpdateGraph(Guid CorrelationId, Graph Graph, List<Common.Domain.Models.Graphs.Map>? Maps = null) : CorrelatedBy<Guid>;