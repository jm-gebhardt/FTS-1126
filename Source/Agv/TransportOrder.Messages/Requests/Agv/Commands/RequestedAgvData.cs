namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;

/// <summary>
/// Defines a result for the EmitRequestData request
/// </summary>
// ReSharper disable once NotAccessedPositionalProperty.Global
public record RequestedAgvData(Guid AgvCorrelationId);