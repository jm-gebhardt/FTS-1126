namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos
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
