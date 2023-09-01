using TransportOrder.Messages.Commands.Agv;

namespace TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Represents the response of the <see cref="SetAgvForceIdle"/>
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record SetAgvForceIdleResult(Guid CorrelationId);