using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Filters;

/// <summary>
/// Filter by changes in <see cref="MqttOrderEvent"/>.
/// </summary>
public class MqttOrderEventSignificanceChecker : ISignificanceChecker<MqttOrderEvent>
{
    /// <inheritdoc cref="ISignificanceChecker{T}"/>>
    public bool HasSignificantChanges(MqttOrderEvent obj1, MqttOrderEvent obj2)
    {
        if (!string.Equals(obj1.SerialNumber, obj2.SerialNumber, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        if (!obj1.OrderEventType.Equals(obj2.OrderEventType))
        {
            return true;
        }

        if (!obj1.OrderId.Equals(obj2.OrderId))
        {
            return true;
        }

        return false;
    }
}