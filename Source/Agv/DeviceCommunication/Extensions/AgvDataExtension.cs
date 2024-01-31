using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Extensions;

/// <summary>
/// Provides extension methods for the agv data.
/// </summary>
public static class AgvDataExtension
{
    /// <summary>
    /// Applies a factor to all velocities and acceleration.
    /// </summary>
    /// <param name="agvData">The agv data.</param>
    /// <param name="simulationSpeed">The simulation speed factor.</param>
    /// <returns></returns>
    public static AgvData SetSimulationSpeed(this AgvData agvData, int simulationSpeed)
    {
        return new AgvData(agvData.CorrelationId,
            agvData.Model,
            agvData.Type,
            agvData.IpV4Address,
            agvData.SoftwareVersion,
            agvData.BatteryCapacity,
            agvData.Acceleration * simulationSpeed,
            agvData.MaxVelocityStraight * simulationSpeed,
            agvData.MaxVelocityTurn * simulationSpeed,
            agvData.MaxVelocityTurnOnSpot * simulationSpeed,
            agvData.TurnOnSpotThreshold,
            agvData.Width,
            agvData.Length,
            agvData.Payload,
            agvData.Sensors);
    }
}