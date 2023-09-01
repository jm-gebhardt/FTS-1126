namespace TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Defines a result for the EmitRequestData request
/// </summary>
// ReSharper disable once NotAccessedPositionalProperty.Global
public record GotAgvData(Guid AgvCorrelationId);