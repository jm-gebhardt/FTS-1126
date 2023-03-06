using static Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvData;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos
{
    /// <summary>
    /// Agv Data model
    /// </summary>
    /// <param name="CorrelationId">The CorrelationId of the agv</param>
    /// <param name="Model">The model name of the AGV</param>
    /// <param name="Type">The type of the AGV</param>
    /// <param name="IpV4Address">The IP Address of the AGV</param>
    /// <param name="BatteryCapacity">The capacity of the AGVs battery in [Ah]</param>
    /// <param name="Acceleration">The maximum velocity of the AGV while turning on spot [m/s^2]</param>
    /// <param name="MaxVelocityStraight">The maximum velocity of the AGV in straight direction [m/s]</param>
    /// <param name="MaxVelocityTurn">The maximum velocity of the AGV in while turning [m/s]</param>
    /// <param name="MaxVelocityTurnOnSpot">The maximum velocity of the AGV while turning on spot [rad/s]</param>
    /// <param name="Width">The width of the AGV in [m]</param>
    /// <param name="Length">The length of the AGV in [m]</param>
    /// <param name="Payload">The max. payload of the AGV in [kg]</param>
    /// <param name="Sensors">The list of sensors in the agv</param>
    /// <returns></returns>
    [JetBrains.Annotations.PublicAPI]
    public record AgvDataDto(
            Guid CorrelationId,
            string Model,
            string Type,
            string? IpV4Address,
            double BatteryCapacity,
            double? Acceleration,
            double MaxVelocityStraight,
            double? MaxVelocityTurn,
            double? MaxVelocityTurnOnSpot,
            double? Width,
            double? Length,
            long? Payload,
            Sensor[] Sensors
        );
}