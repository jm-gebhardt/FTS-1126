namespace Gebhardt.Acc.TransportOrder.Messages.MessageContent;

/// <summary>
/// Agv has cargo
/// </summary>
/// <param name="CorrelationId">The correlation id of the Agv.</param>
/// <param name="HasCargo">True if there is any cargo on the Agv.</param>
public record AgvHasCargo(Guid CorrelationId, bool HasCargo);