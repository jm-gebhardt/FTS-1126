using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;

/// <summary>
/// Provides a request for the latest VDA5050 state of an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
[PublicAPI]
public record RequestAgvState(Guid CorrelationId) : CorrelatedBy<Guid>;