using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper;

/// <summary>
/// Provides a generator of <see cref="MqttConnection"/> messages for unit testing.
/// </summary>
public static class MqttConnectionGenerator
{
    /// <summary>
    /// Generate connection message.
    /// </summary>
    /// <param name="serialNumber">Optional agv serial number.</param>
    /// <param name="connectionState">Optional connection state.</param>
    /// <returns></returns>
    public static MqttConnection Generate(string serialNumber = "agv01", MqttConnectionState connectionState = MqttConnectionState.Online)
    {
        return new MqttConnection{SerialNumber = serialNumber, ConnectionState = connectionState};
    }
}