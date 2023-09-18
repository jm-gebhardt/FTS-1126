namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Defines a result for the EmitRequestGraph request
/// </summary>
// ReSharper disable once NotAccessedPositionalProperty.Global
public record RequestAgvGraphResult(Guid AgvCorrelationId);