namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos
{
    /// <summary>
    /// AgvState model
    /// </summary>
    /// <param name="CorrelationId">The CorrelationId of the agv</param>
    /// <param name="AgvId">AGV Serial Number</param>
    /// <param name="CurrentState">The current state of the AGV</param>
    /// <param name="IsOnline">Returns true when there is a life sign of the AGV in regular time intervals, returns false when there is no life sign for a certain time.</param>
    /// <param name="AgvStatus">AGV Status</param>
    /// <param name="IsLocked">Agv lock status</param>
    [JetBrains.Annotations.PublicAPI]
    public record AgvStateDto(
        Guid CorrelationId,
        string AgvId,
        string CurrentState,
        bool IsOnline,
        bool IsLocked,
        AgvStatusDto AgvStatus) : AgvDto(CorrelationId, AgvId, CurrentState, IsOnline, IsLocked);
}