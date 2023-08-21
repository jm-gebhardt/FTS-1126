namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;

/// <summary>
/// Defines a result for the EmitRequestGraph request
/// </summary>
// ReSharper disable once NotAccessedPositionalProperty.Global
public record RequestAgvGraphResult(Guid AgvCorrelationId);