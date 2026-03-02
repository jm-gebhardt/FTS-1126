using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper;

/// <summary>
/// Provides a generator for <see cref="MqttState"/> and adjoint messages.
/// </summary>
[PublicAPI]
public static class MqttStateGenerator
{
    /// <summary>
    /// Generate a state for an AGV.
    /// </summary>
    /// <param name="serialNumber">Serial number of the AGV.</param>
    /// <param name="nodeStates">Optional node states.</param>
    /// <param name="edgeStates">Optional edge states.</param>
    /// <param name="actionStates">Optional action states.</param>
    /// <param name="errors">Optional errors.</param>
    /// <param name="position">Optional agv position</param>
    /// <param name="cargo">Optional load of the agv.</param>
    /// <param name="infos">Optional agv information</param>
    public static MqttState Generate(
        string serialNumber = "agv01",
        IList<MqttNodeState>? nodeStates = null,
        IList<MqttEdgeState>? edgeStates = null,
        IList<MqttActionState>? actionStates = null,
        IList<MqttError>? errors = null,
        MqttAgvPosition? position = null,
        IList<MqttLoad>? cargo = null,
        IList<MqttInformation>? infos = null)
    {
        return new MqttState
        {
            SerialNumber = serialNumber,
            HeaderId = 1,
            Timestamp = DateTimeOffset.UtcNow,
            Version = "2.0.0",
            Manufacturer = "Gebhardt",
            OrderId = string.Empty,
            OrderUpdateId = 0,
            LastNodeSequenceId = 0,
            Driving = false,
            OperatingMode = MqttOperatingMode.Automatic,
            NodeStates = nodeStates ?? [],
            EdgeStates = edgeStates ?? [],
            ActionStates = actionStates ?? [],
            BatteryState = GenerateBatteryState(),
            Errors = errors ?? [],
            SafetyState = GenerateSafetyState(),
            AgvPosition = position,
            Loads = cargo ?? [GenerateLoad()],
            Information = infos ?? [GenerateInformation()]
        };
    }

    public static MqttNodeState GenerateNodeState(string nodeId = "node1", int sequenceId = 0, bool released = true)
    {
        return new MqttNodeState { NodeId = nodeId, SequenceId = sequenceId, Released = released };
    }

    public static MqttEdgeState GenerateEdgeState(string edgeId = "edge1", int sequenceId = 1, bool released = true)
    {
        return new MqttEdgeState { EdgeId = edgeId, SequenceId = sequenceId, Released = released };
    }

    public static MqttActionState GenerateActionState(string actionId = "action1", MqttActionStatus actionStatus = MqttActionStatus.Waiting)
    {
        return new MqttActionState { ActionId = actionId, ActionStatus = actionStatus };
    }

    public static MqttBatteryState GenerateBatteryState(double batteryCharge = 80.0, bool charging = false)
    {
        return new MqttBatteryState { BatteryCharge = batteryCharge, Charging = charging };
    }

    public static MqttSafetyState GenerateSafetyState(MqttEStop eStop = MqttEStop.None, bool fieldViolation = false)
    {
        return new MqttSafetyState { EStop = eStop, FieldViolation = fieldViolation };
    }

    public static MqttError GenerateError(string errorType = "error", MqttErrorLevel errorLevel = MqttErrorLevel.Warning)
    {
        return new MqttError { ErrorType = errorType, ErrorLevel = errorLevel };
    }

    public static MqttLoad GenerateLoad(string loadId = "load1", string loadType = "pallet", string loadPosition = "loadPos1")
    {
        return new MqttLoad { LoadId = loadId, LoadType = loadType, LoadPosition = loadPosition };
    }

    public static MqttInformation GenerateInformation(string infoType = "info", MqttInfoLevel infoLevel = MqttInfoLevel.Info)
    {
        return new MqttInformation { InfoType = infoType, InfoLevel = infoLevel };
    }
}
