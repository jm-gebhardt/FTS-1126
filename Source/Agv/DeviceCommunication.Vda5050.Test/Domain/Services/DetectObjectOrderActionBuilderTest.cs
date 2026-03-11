using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

[TestClass]
public class DetectObjectOrderActionBuilderTest
{
    private readonly DetectObjectOrderActionBuilder _builder = new();

    [TestMethod]
    public void ActionType_ReturnsDetectObjectConstant()
    {
        _builder.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.DetectObject);
    }

    [TestMethod]
    public void Build_ObjectTypeNotRequired_ReturnsActionWithNoParameters()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.DetectObject,
            ActionParameters = [],
            BlockingTypes = [BlockingType.SOFT]
        };
        var context = new OrderActionContext([], BlockingType.SOFT, true);

        var result = _builder.Build(agvAction, context);

        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.DetectObject);
        result.ActionParameters!.ShouldBeEmpty();
    }

    [TestMethod]
    public void Build_ObjectTypeOptional_DoesNotAddObjectTypeParameter()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.DetectObject,
            ActionParameters = [new AgvActionParameter { Key = Vda5050Specifications.Parameters.ObjectType, IsOptional = true }],
            BlockingTypes = [BlockingType.SOFT]
        };
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.ObjectType, Value = "pallet" }],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters!.ShouldBeEmpty();
    }

    [TestMethod]
    public void Build_ObjectTypeRequired_AddsObjectTypeParameter()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.DetectObject,
            ActionParameters = [new AgvActionParameter { Key = Vda5050Specifications.Parameters.ObjectType, IsOptional = false }],
            BlockingTypes = [BlockingType.SOFT]
        };
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.ObjectType, Value = "pallet" }],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters!.Count.ShouldBe(1);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.ObjectType).Value.ShouldBe("pallet");
    }
}
