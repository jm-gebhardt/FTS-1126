namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;

/// <summary>
/// Defines a result for the LockAgv request
/// </summary>
// ReSharper disable once NotAccessedPositionalProperty.Global
public record LockAgvResult(Guid AgvCorrelationId);