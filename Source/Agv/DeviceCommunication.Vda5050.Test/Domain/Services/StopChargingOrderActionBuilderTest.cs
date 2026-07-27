using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

[TestClass]
public class StopChargingOrderActionBuilderTest
{
    private readonly StopChargingOrderActionBuilder _builder = new();

    [TestMethod]
    public void ActionType_ReturnsStopChargingConstant()
    {
        _builder.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.StopCharging);
    }

    [TestMethod]
    public void Build_ReturnsActionWithCorrectTypeAndNoParameters()
    {
        var agvAction = ActionGenerator.CreateAction(Vda5050Specifications.ActionTypes.StopCharging, blockingTypes: [BlockingType.SOFT]);
        var context = new OrderActionContext([], BlockingType.SOFT, true);

        var result = _builder.Build(agvAction, context);

        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.StopCharging);
        result.ActionParameters.ShouldNotBeNull().ShouldBeEmpty();
    }

    [TestMethod]
    public void Build_ReturnsActionWithResolvedBlockingType()
    {
        var agvAction = ActionGenerator.CreateAction(Vda5050Specifications.ActionTypes.StopCharging, blockingTypes: [BlockingType.HARD]);
        var context = new OrderActionContext([], BlockingType.HARD, true);

        var result = _builder.Build(agvAction, context);

        result.BlockingType.ShouldBe(BlockingType.HARD);
    }

    [TestMethod]
    public void Build_NoBlockingTypesOnAgvAction_ReturnsFallbackBlockingTypeHard()
    {
        var agvAction = ActionGenerator.CreateAction(Vda5050Specifications.ActionTypes.StopCharging);
        var context = new OrderActionContext([], BlockingType.SOFT, true);

        var result = _builder.Build(agvAction, context);

        result.BlockingType.ShouldBe(BlockingType.HARD);
    }
}