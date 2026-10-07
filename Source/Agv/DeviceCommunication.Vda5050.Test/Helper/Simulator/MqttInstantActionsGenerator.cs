using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;

public static class MqttInstantActionsGenerator
{
    public const string FactsheetRequestActionId = "factsheetRequest1";
    public const string SecondFactsheetRequestActionId = "factsheetRequest2";

    public static MqttInstantActions Generate(
        string manufacturer = "gebhardt",
        string serialNumber = "simagv0",
        ICollection<MqttInstantAction>? actions = null)
    {
        return new MqttInstantActions
        {
            HeaderId = 1,
            Timestamp = DateTimeOffset.UtcNow,
            Version = Vda5050Specifications.Version,
            Manufacturer = manufacturer,
            SerialNumber = serialNumber,
            Actions = actions ?? []
        };
    }

    public static MqttInstantAction GenerateInitPositionAction(
        float x = 1.0f,
        float y = 2.0f,
        float theta = 0.0f,
        string? lastNodeId = null)
    {
        var parameters = new List<MqttActionParameter>
        {
            new() { Key = Vda5050Specifications.Parameters.X, Value = x },
            new() { Key = Vda5050Specifications.Parameters.Y, Value = y },
            new() { Key = Vda5050Specifications.Parameters.Theta, Value = theta }
        };
        if (lastNodeId != null)
        {
            parameters.Add(new MqttActionParameter { Key = Vda5050Specifications.Parameters.LastNodeId, Value = lastNodeId });
        }
        return new MqttInstantAction
        {
            ActionType = Vda5050Specifications.ActionTypes.InitPosition,
            ActionId = Guid.NewGuid().ToString(),
            ActionParameters = parameters
        };
    }

    public static MqttInstantAction GenerateStartChargingAction()
    {
        return new MqttInstantAction
        {
            ActionType = Vda5050Specifications.ActionTypes.StartCharging,
            ActionId = Guid.NewGuid().ToString()
        };
    }

    public static MqttInstantAction GenerateStopChargingAction()
    {
        return new MqttInstantAction
        {
            ActionType = Vda5050Specifications.ActionTypes.StopCharging,
            ActionId = Guid.NewGuid().ToString()
        };
    }

    public static MqttInstantAction GenerateCancelOrderAction(string actionId = "cancelAction1")
    {
        return new MqttInstantAction
        {
            ActionType = Vda5050Specifications.ActionTypes.CancelOrder,
            ActionId = actionId
        };
    }

    public static MqttInstantAction GenerateFactsheetRequestAction(string actionId = FactsheetRequestActionId)
    {
        return new MqttInstantAction
        {
            ActionType = Vda5050Specifications.ActionTypes.RequestFactsheet,
            ActionId = actionId
        };
    }
}