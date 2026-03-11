using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

[TestClass]
public class OrderActionBuilderRegistryTest
{
    private static OrderActionBuilderRegistry CreateRegistry()
        => new([
            new StartChargingOrderActionBuilder(),
            new StopChargingOrderActionBuilder()
        ]);

    private static AgvAction CreateAgvAction(string actionType) => new()
    {
        ActionType = actionType,
        BlockingTypes = [BlockingType.SOFT]
    };

    [TestMethod]
    public void Build_KnownActionType_DispatchesToRegisteredBuilder()
    {
        var registry = CreateRegistry();
        var agvAction = CreateAgvAction(Vda5050Specifications.ActionTypes.StartCharging);
        var context = new OrderActionContext([], BlockingType.SOFT, true);

        var result = registry.Build(agvAction, context);

        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.StartCharging);
    }

    [TestMethod]
    public void Build_UnknownActionType_BuildsCustomAction()
    {
        var registry = CreateRegistry();
        var agvAction = CreateAgvAction("customAction");
        var context = new OrderActionContext([], null, false);

        var result = registry.Build(agvAction, context);

        result.ActionType.ShouldBe("customAction");
        result.ActionParameters!.ShouldBeEmpty();
    }

    [TestMethod]
    public void Build_UnknownActionType_WithCustomParameters_AddsParametersToAction()
    {
        var registry = CreateRegistry();
        var agvAction = CreateAgvAction("customAction");
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = "param1", Value = "value1" }],
            null,
            false);

        var result = registry.Build(agvAction, context);

        result.ActionType.ShouldBe("customAction");
        result.ActionParameters!.Count.ShouldBe(1);
        result.ActionParameters.Single(p => p.Key == "param1").Value.ShouldBe("value1");
    }

    [TestMethod]
    public void Build_CaseInsensitiveActionType_FindsRegisteredBuilder()
    {
        var registry = CreateRegistry();
        var agvAction = CreateAgvAction("STARTCHARGING");
        var context = new OrderActionContext([], BlockingType.SOFT, true);

        var result = registry.Build(agvAction, context);

        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.StartCharging);
    }

    [TestMethod]
    public void Build_NodeAction_ReturnsOrderActionFromNodeAction()
    {
        var registry = CreateRegistry();
        var nodeAction = new AgvNodeAction
        {
            ActionType = Vda5050Specifications.ActionTypes.StartCharging,
            BlockingType = BlockingType.HARD,
            Parameters = [new OrderActionParameter { Key = "key1", Value = "val1" }]
        };

        var result = registry.Build(nodeAction);

        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.StartCharging);
        result.BlockingType.ShouldBe(BlockingType.HARD);
        result.ActionParameters.ShouldBe(nodeAction.Parameters);
    }

    [TestMethod]
    public void Build_EdgeAction_ReturnsOrderActionFromEdgeAction()
    {
        var registry = CreateRegistry();
        var edgeAction = new AgvEdgeAction
        {
            ActionType = Vda5050Specifications.ActionTypes.WaitForTrigger,
            BlockingType = BlockingType.SOFT,
            Parameters = [new OrderActionParameter { Key = "triggerType", Value = "signal" }]
        };

        var result = registry.Build(edgeAction);

        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.WaitForTrigger);
        result.BlockingType.ShouldBe(BlockingType.SOFT);
        result.ActionParameters.ShouldBe(edgeAction.Parameters);
    }

    [TestMethod]
    public void Build_NodeAction_NullDescription_ReturnsOrderActionWithNullDescription()
    {
        var registry = CreateRegistry();
        var nodeAction = new AgvNodeAction
        {
            ActionType = Vda5050Specifications.ActionTypes.StopCharging,
            BlockingType = BlockingType.SOFT,
            Parameters = []
        };

        var result = registry.Build(nodeAction);

        result.ActionDescription.ShouldBeNull();
    }
}
