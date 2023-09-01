namespace TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to set an agv to idle by force.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record SetAgvForceIdle(Guid CorrelationId);