using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;

/// <summary>
/// Request an AGV to transition to busy state
/// </summary>
/// <param name="CorrelationId">The Correlation of the Agv</param>
/// <param name="IgnoreLock">If true the IsLocked of the Agv maybe true</param>
[PublicAPI]
public record SetAgvBusy(Guid CorrelationId, bool IgnoreLock) : CorrelatedBy<Guid>;