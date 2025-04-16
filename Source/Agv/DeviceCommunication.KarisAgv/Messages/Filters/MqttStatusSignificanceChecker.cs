using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Filters;

/// <summary>
/// Filter by changes in <see cref="MqttState"/>.
/// </summary>
public class MqttStatusSignificanceChecker : ISignificanceChecker<MqttState>
{
    private const float Epsilon = 0.00001f;

    /// <inheritdoc cref="ISignificanceChecker{T}"/>>
    public bool HasSignificantChanges(MqttState obj1, MqttState obj2)
    {
        if (!string.Equals(obj1.SerialNumber, obj2.SerialNumber, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        if (!string.Equals(obj1.OrderId, obj2.OrderId, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        if (!string.Equals(obj1.LastNodeId, obj2.LastNodeId, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        if (!string.Equals(obj1.Manufacturer, obj2.Manufacturer, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        if (!string.Equals(obj1.Version, obj2.Version, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        if (!string.Equals(obj1.ZoneSetId, obj2.ZoneSetId, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        if (obj1.LastNodeSequenceId != obj2.LastNodeSequenceId)
        {
            return true;
        }

        if (obj1.OrderUpdateId != obj2.OrderUpdateId)
        {
            return true;
        }

        if (obj1.Driving != obj2.Driving)
        {
            return true;
        }

        if (obj1.OperatingMode != obj2.OperatingMode)
        {
            return true;
        }

        if (Math.Abs(obj1.DistanceSinceLastNode.GetValueOrDefault() - obj2.DistanceSinceLastNode.GetValueOrDefault()) > Epsilon)
        {
            return true;
        }

        if (obj1.NewBaseRequest != obj2.NewBaseRequest)
        {
            return true;
        }

        if (obj1.Paused != obj2.Paused)
        {
            return true;
        }

        if (!(obj1.AgvPosition?.Equals(obj2.AgvPosition) ?? obj2.AgvPosition == null))
        {
            return true;
        }

        if (!(obj1.BatteryState?.Equals(obj2.BatteryState) ?? obj2.BatteryState == null))
        {
            return true;
        }

        if (!(obj1.SafetyState?.Equals(obj2.SafetyState) ?? obj2.SafetyState == null))
        {
            return true;
        }

        if (!(obj1.Velocity?.Equals(obj2.Velocity) ?? obj2.Velocity == null))
        {
            return true;
        }

        // Compare lists (null-safe and deep comparison)
        if (!AreListsEqual(obj1.ActionStates, obj2.ActionStates))
        {
            return true;
        }

        if (!AreListsEqual(obj1.EdgeStates, obj2.EdgeStates))
        {
            return true;
        }

        if (!AreListsEqual(obj1.Errors, obj2.Errors))
        {
            return true;
        }

        if (!AreListsEqual(obj1.Information, obj2.Information))
        {
            return true;
        }

        if (!AreListsEqual(obj1.Loads, obj2.Loads))
        {
            return true;
        }

        if (!AreListsEqual(obj1.NodeStates, obj2.NodeStates))
        {
            return true;
        }

        if (!AreListsEqual(obj1.ZoneIds, obj2.ZoneIds))
        {
            return true;
        }

        return false;
    }

    private bool AreListsEqual<T>(List<T>? list1, List<T>? list2)
    {
        if (ReferenceEquals(list1, list2))
        {
            return true;
        }
        if (list1 == null || list2 == null)
        {
            return false;
        }
        if (list1.Count != list2.Count)
        {
            return false;
        }
        for (int i = 0; i < list1.Count; i++)
        {
            if (!(list1[i]?.Equals(list2[i]) ?? list2[i] == null))
            {
                return false;
            }
        }
        return true;
    }
}