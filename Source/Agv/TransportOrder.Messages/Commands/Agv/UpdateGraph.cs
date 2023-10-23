using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to update an agv graph.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="AgvGraph">The graph.</param>
public record UpdateGraph(Guid CorrelationId, AgvGraph AgvGraph) : CorrelatedBy<Guid>;