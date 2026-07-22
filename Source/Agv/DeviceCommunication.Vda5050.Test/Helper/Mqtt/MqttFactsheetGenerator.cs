using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Mqtt;

/// <summary>
/// Provides a generator for <see cref="MqttFactsheet"/> and adjoint messages.
/// </summary>
public static class MqttFactsheetGenerator
{
    /// <summary>
    /// Generate factsheet for agv.
    /// </summary>
    public static MqttFactsheet Generate(string serialNumber = "agv01", ICollection<MqttAgvAction>? actions = null, string seriesName = "series", float width =1)
    {
        return new MqttFactsheet
        {
            SerialNumber = serialNumber,
            TypeSpecification = new MqttTypeSpecification { SeriesName = seriesName, AgvClass = MqttAgvClass.Carrier, MaxLoadMass = 100 },
            VehicleConfig = new MqttVehicleConfig { Network = new MqttNetworkConfig { LocalIpAddress = "127.0.0.1" }, Versions = [new MqttVehicleVersion { Key = "softwareVersion", Value = "1.0.0" }] },
            PhysicalParameters = new MqttPhysicalParameters { AccelerationMax = 1.0, SpeedMax = 1.5, Width = width, Length = 1.0 },
            ProtocolFeatures = new MqttProtocolFeatures { AgvActions = actions ?? [] }
        };
    }

    public static MqttAgvAction GenerateAgvAction(string actionType = "pick", MqttActionScope scope = MqttActionScope.Node)
    {
        return new MqttAgvAction { ActionType = actionType, ActionScopes = [scope] };
    }
}