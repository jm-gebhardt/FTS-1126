using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Event triggered every time the AgvGraph has changed / was received.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="AgvGraph">The agv graph.</param>
public record AgvGraphChanged(Guid CorrelationId, AgvGraph AgvGraph) : CorrelatedBy<Guid>;