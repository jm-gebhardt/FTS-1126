namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;

/// <summary>
/// Defines a result for the unlock agv request
/// </summary>
// ReSharper disable once NotAccessedPositionalProperty.Global
public record UnlockAgvResult(Guid AgvCorrelationId);