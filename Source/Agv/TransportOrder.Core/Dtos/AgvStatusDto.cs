namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos
{
    /// <summary>
    /// AgvStatus model
    /// </summary>
    /// <param name="OrderId"></param>
    /// <param name="Busy"></param>
    /// <param name="Automatic"></param>
    /// <param name="BatteryCharge"></param>
    /// <param name="Charging"></param>
    /// <param name="Disturbed"></param>
    /// <param name="LastNodeId"></param>
    [JetBrains.Annotations.PublicAPI]
    public record AgvStatusDto(
        string? OrderId,
        bool? Busy,
        bool? Automatic,
        int? BatteryCharge,
        bool? Charging,
        bool? Disturbed,
        string? LastNodeId);
}