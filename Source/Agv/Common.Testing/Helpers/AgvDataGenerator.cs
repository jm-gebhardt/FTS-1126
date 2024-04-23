using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Common.Testing.Helpers;

public static class AgvDataGenerator
{
    public static AgvData GenerateAgvData(Guid? correlationId = null,
        double width = 2.0,
        string ipV4Address = "127.0.0.1",
        double batteryCapacity = 70,
        double length = 1.0,
        double acceleration = 0.2,
        double maxVelocityStraight = 0.6,
        double maxVelocityTurn = 0.8,
        double maxVelocityTurnOnSpot = 0.7,
        double turnOnSpotThreshold = 0.0872664, // 5°
        AgvData.Sensor[]? sensors = null)
    {
        return new AgvData(correlationId ?? Guid.NewGuid(),
            "Karis",
            "BF1",
            ipV4Address,
            "v0",
            batteryCapacity,
            acceleration,
            maxVelocityStraight,
            maxVelocityTurn,
            maxVelocityTurnOnSpot,
            turnOnSpotThreshold,
            width,
            length,
            1000,
            sensors ?? Array.Empty<AgvData.Sensor>());
    }
}