namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Agv has cargo.
/// </summary>
/// <param name="CorrelationId">The correlation id of the Agv.</param>
/// <param name="HasCargo">True if there is any cargo on the Agv.</param>
public record AgvHasCargoDto(Guid CorrelationId, bool HasCargo);