using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper;

/// <summary>
/// Provides a generator for <see cref="MqttVisualization"/> and adjoint messages.
/// </summary>
public static class MqttVisualizationGenerator
{
    /// <summary>
    /// Generate an agv position.
    /// </summary>
    /// <param name="mapId">The map id.</param>
    /// <returns>A <see cref="MqttAgvPosition"/> on the given map with arbitrary coordinates.</returns>
    public static MqttAgvPosition GeneratePosition(string mapId = "map")
    {
        return new MqttAgvPosition() { MapId = mapId, X = 1, Y = 1, PositionInitialized = true, Theta = 0.5 };
    }

    public static MqttVisualization Generate(string? serialNumber="agv01")
    {
        return new MqttVisualization() { SerialNumber = serialNumber, AgvPosition = GeneratePosition()};
    }
}