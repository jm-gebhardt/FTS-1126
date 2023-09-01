using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using MassTransit;

namespace TransportOrder.Messages.Events.Agv;

/// <summary>
/// Event triggered every time the AgvGraph has changed / was received
/// </summary>
/// <param name="CorrelationId"></param>
/// <param name="AgvGraph"></param>
[JetBrains.Annotations.PublicAPI]
public record AgvGraphChanged(Guid CorrelationId, AgvGraph AgvGraph) : CorrelatedBy<Guid>;
