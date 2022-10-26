
// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos
{
    /// <summary>
    /// AGV model
    /// </summary>
    /// <param name="CorrelationId">CorrelationId</param>
    /// <param name="AgvId">AGV Serial Number</param>
    /// <param name="CurrentState">Current State</param>
    [JetBrains.Annotations.PublicAPI]
    public record AgvDto(Guid CorrelationId, string AgvId, string CurrentState);
}
