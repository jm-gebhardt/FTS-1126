using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
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
        var agvAction = ActionGenerator.CreateAction(Vda5050Specifications.ActionTypes.DetectObject, [], [BlockingType.SOFT]);
        var context = new OrderActionContext([], BlockingType.SOFT, true);

        var result = _builder.Build(agvAction, context);

        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.DetectObject);
        result.ActionParameters.ShouldNotBeNull().ShouldBeEmpty();
    }

    [TestMethod]
    public void Build_ObjectTypeOptional_DoesNotAddObjectTypeParameter()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.DetectObject,
            [ActionGenerator.OptionalParameter(Vda5050Specifications.Parameters.ObjectType)],
            [BlockingType.SOFT]);
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.ObjectType, Value = "pallet" }],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters.ShouldNotBeNull().ShouldBeEmpty();
    }

    [TestMethod]
    public void Build_ObjectTypeRequired_AddsObjectTypeParameter()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.DetectObject,
            [ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.ObjectType)],
            [BlockingType.SOFT]);
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.ObjectType, Value = "pallet" }],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters.ShouldNotBeNull().Count.ShouldBe(1);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.ObjectType).Value.ShouldBe("pallet");
    }
}