using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunicationTest.Helper;

/// <summary>
/// Provides a test helper for generating <see cref="MqttState"/> messages.
/// </summary>
[PublicAPI]
public class MqttStateGenerator
{
    /// <summary>
    /// Generate a <see cref="MqttState"/> message
    /// </summary>
    /// <param name="serialNumber">The agv serial number.</param>
    /// <param name="orderId">The order id.</param>
    /// <param name="actionStates">The current action states.</param>
    /// <param name="errors">The agv errors.</param>
    /// <param name="information">The agv information.</param>
    /// <param name="lastNodeId">The last node id.</param>
    /// <param name="loads">The agv loads.</param>
    /// <param name="mode">The agv operation mode.</param>
    /// <param name="safetyState">The agv safety state.</param>
    /// <param name="stationIds">The agv station ids.</param>
    public static MqttState GenerateState(string serialNumber,
        string orderId,
        List<MqttActionState>? actionStates = null,
        List<MqttError>? errors = null,
        List<MqttInformation>? information = null,
        string? lastNodeId = "a node",
        List<MqttLoad>? loads = null,
        MqttOperatingMode mode = MqttOperatingMode.Automatic,
        MqttSafetyState? safetyState = null,
        List<string>? stationIds = null)
    {
        return new MqttState(serialNumber, orderId)
        {
            ActionStates = actionStates,
            AgvPosition = new MqttAgvPosition
            {
                DeviationRange = 0,
                LocalizationScore = 1.0f,
                MapDescription = "a map description",
                MapId = "mapId",
                PositionInitialized = true,
                Theta = 0,
                X = 1,
                Y = 1
            },
            BatteryState = new MqttBatteryState()
            {
                BatteryCharge = 15,
                BatteryCurrent = -12,
                BatteryHealth = 30,
                BatteryTemperature = 40,
                BatteryVoltage = 2,
                Charging = false,
                Reach = 23
            },
            Errors = errors,
            Information = information,
            LastNodeId = lastNodeId,
            Loads = loads,
            OperatingMode = mode,
            SafetyState = safetyState,
            StationIds = stationIds
        };
    }
}