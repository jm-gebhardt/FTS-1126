using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;

/// <summary>
/// Provides a generator for the simulator's <see cref="MqttState"/>.
/// </summary>
[PublicAPI]
public static class MqttStateGenerator
{
    public static MqttState Generate(
        string serialNumber = "simagv0",
        double batteryCharge = 80.0,
        MqttOperatingMode operatingMode = MqttOperatingMode.Automatic,
        MqttAgvPosition? position = null,
        IList<MqttNodeState>? nodeStates = null,
        IList<MqttEdgeState>? edgeStates = null,
        IList<MqttActionState>? actionStates = null,
        IList<MqttError>? errors = null,
        IList<MqttLoad>? loads = null)
    {
        return new MqttState
        {
            HeaderId = 1,
            Timestamp = DateTimeOffset.UtcNow,
            Version = "2.0.0",
            Manufacturer = "gebhardt",
            SerialNumber = serialNumber,
            OrderId = string.Empty,
            OrderUpdateId = 0,
            LastNodeSequenceId = 0,
            Driving = false,
            OperatingMode = operatingMode,
            NodeStates = nodeStates ?? [],
            EdgeStates = edgeStates ?? [],
            ActionStates = actionStates ?? [],
            BatteryState = GenerateBatteryState(batteryCharge),
            Errors = errors ?? [],
            SafetyState = GenerateSafetyState(),
            AgvPosition = position ?? GenerateAgvPosition(),
            Loads = loads
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

    public static MqttAgvPosition GenerateAgvPosition(double x = 0.0, double y = 0.0, double theta = 0.0, bool positionInitialized = true)
    {
        return new MqttAgvPosition { X = x, Y = y, Theta = theta, PositionInitialized = positionInitialized };
    }
}