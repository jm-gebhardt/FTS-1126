namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common
{
    /// <summary>
    /// Defines a result for the EmitRequestData request
    /// </summary>
    // ReSharper disable once NotAccessedPositionalProperty.Global
    public record RequestAgvDataResult(Guid AgvCorrelationId);
}
