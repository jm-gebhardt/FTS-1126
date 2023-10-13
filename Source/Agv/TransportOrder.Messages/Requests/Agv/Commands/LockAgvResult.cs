namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;

/// <summary>
/// Defines a result for the LockAgv request
/// </summary>
// ReSharper disable once NotAccessedPositionalProperty.Global
public record LockAgvResult(Guid AgvCorrelationId);