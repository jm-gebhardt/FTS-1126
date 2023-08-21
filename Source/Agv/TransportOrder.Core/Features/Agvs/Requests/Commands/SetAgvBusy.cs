using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands;

/// <summary>
/// Request an AGV to transition to busy state
/// </summary>
/// <param name="CorrelationId">The Correlation of the Agv</param>
/// <param name="IgnoreLock">If true the IsLocked of the Agv maybe true</param>
[JetBrains.Annotations.PublicAPI]
public record SetAgvBusy(Guid CorrelationId, bool IgnoreLock) : CorrelatedBy<Guid>;