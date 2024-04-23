using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;

/// <summary>
/// Represents the response of the <see cref="SetAgvForceIdle"/>
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record SetAgvForceIdleResult(Guid CorrelationId);