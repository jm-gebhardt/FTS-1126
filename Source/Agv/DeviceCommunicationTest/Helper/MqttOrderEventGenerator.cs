using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunicationTest.Helper;

public static class MqttOrderEventGenerator
{
    private static string GetUniqueAgvSerialNumber() => $"#AGV-{Guid.NewGuid()}";

    private const string OrderId = "ORDER123";

    public static MqttOrderEvent CreateMqttOrderEvent(OrderEventType orderEventType, string? serialNumber = null)
    {
        return new MqttOrderEvent($"{OrderId.ToGuid()} {OrderId}", serialNumber ?? GetUniqueAgvSerialNumber(), orderEventType);
    }
}