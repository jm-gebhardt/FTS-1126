using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Graphs.Requests;

/// <summary>
/// Command to update the (global) graph. Will be responded with a <see cref="Success"/> message if the graph was updated successfully.
/// </summary>
/// <param name="CorrelationId">The correlation Id of the Agv.</param>
/// <param name="Graph">The new (global) Graph.</param>
/// <param name="Maps">Optionally, you can already specify maps.</param>
public record UpdateGraphRequest(Guid CorrelationId, Graph Graph, List<Map>? Maps = null) : CorrelatedBy<Guid>;