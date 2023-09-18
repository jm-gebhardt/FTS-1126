using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Event triggered every time the AgvGraph has changed / was received
/// </summary>
/// <param name="CorrelationId"></param>
/// <param name="AgvGraph"></param>
[PublicAPI]
public record AgvGraphChanged(Guid CorrelationId, AgvGraph AgvGraph) : CorrelatedBy<Guid>;
