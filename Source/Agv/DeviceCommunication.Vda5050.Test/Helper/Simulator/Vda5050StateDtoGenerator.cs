using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;

/// <summary>
/// Provides a generator for <see cref="Vda5050StateDto"/>.
/// </summary>
[PublicAPI]
public static class Vda5050StateDtoGenerator
{
    public static readonly DateTime DefaultMessageCreatedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static Vda5050StateDto Generate(
        Guid? correlationId = null,
        double batteryCharge = 80.0,
        OperatingModeDto operatingMode = OperatingModeDto.Automatic,
        Vda5050AgvPositionDto? agvPosition = null,
        IList<NodeStateDto>? nodeStates = null,
        IList<EdgeStateDto>? edgeStates = null,
        IList<ActionStateDto>? actionStates = null,
        IList<ErrorDto>? errors = null,
        IList<LoadDto>? loads = null)
    {
        return new Vda5050StateDto
        {
            CorrelationId = correlationId ?? Guid.Empty,
            MessageCreatedDate = DefaultMessageCreatedDate,
            SerialNumber = "simagv0",
            OperatingMode = operatingMode,
            BatteryState = new BatteryStateDto { BatteryCharge = batteryCharge },
            AgvPosition = agvPosition ?? GenerateAgvPosition(),
            NodeStates = nodeStates ?? [],
            EdgeStates = edgeStates ?? [],
            ActionStates = actionStates ?? [],
            Errors = errors ?? [],
            Loads = loads
        };
    }

    public static Vda5050AgvPositionDto GenerateAgvPosition(double x = 0.0, double y = 0.0, double theta = 0.0, bool positionInitialized = true)
    {
        return new Vda5050AgvPositionDto { X = x, Y = y, Theta = theta, PositionInitialized = positionInitialized };
    }

    public static NodeStateDto GenerateNodeState(string nodeId = "node1", int sequenceId = 0, bool released = true)
    {
        return new NodeStateDto { NodeId = nodeId, SequenceId = sequenceId, Released = released };
    }

    public static ActionStateDto GenerateActionState(string actionId = "action1", ActionStatusDto actionStatus = ActionStatusDto.Waiting)
    {
        return new ActionStateDto { ActionId = actionId, ActionStatus = actionStatus };
    }

    public static ErrorDto GenerateError(string errorType = "error", ErrorLevelDto errorLevel = ErrorLevelDto.Warning)
    {
        return new ErrorDto { ErrorType = errorType, ErrorLevel = errorLevel };
    }

    public static LoadDto GenerateLoad(string loadId = "load1")
    {
        return new LoadDto { LoadId = loadId };
    }
}