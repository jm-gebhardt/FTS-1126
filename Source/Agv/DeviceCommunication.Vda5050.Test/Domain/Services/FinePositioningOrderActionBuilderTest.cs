using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

[TestClass]
public class FinePositioningOrderActionBuilderTest
{
    private readonly FinePositioningOrderActionBuilder _builder = new();

    [TestMethod]
    public void ActionType_ReturnsFinePositioningConstant()
    {
        _builder.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.FinePositioning);
    }

    [TestMethod]
    public void Build_NoOptionalParameters_ReturnsActionWithNoParameters()
    {
        var agvAction = ActionGenerator.CreateAction(Vda5050Specifications.ActionTypes.FinePositioning, [], [BlockingType.SOFT]);
        var context = new OrderActionContext([], BlockingType.SOFT, true);

        var result = _builder.Build(agvAction, context);

        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.FinePositioning);
        result.ActionParameters.ShouldNotBeNull().ShouldBeEmpty();
    }

    [TestMethod]
    public void Build_WithRequiredStationName_AddsStationNameParameter()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.FinePositioning,
            [ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationName)],
            [BlockingType.SOFT]);
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationName, Value = "PS01" }],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters.ShouldNotBeNull().Count.ShouldBe(1);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.StationName).Value.ShouldBe("PS01");
    }

    [TestMethod]
    public void Build_WithRequiredStationType_AddsStationTypeParameter()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.FinePositioning,
            [ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationType)],
            [BlockingType.SOFT]);
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "rack" }],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters.ShouldNotBeNull().Count.ShouldBe(1);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.StationType).Value.ShouldBe("rack");
    }

    [TestMethod]
    public void Build_WithBothRequiredParameters_AddsBothParameters()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.FinePositioning,
            [
                ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationName),
                ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationType)
            ],
            [BlockingType.SOFT]);
        var context = new OrderActionContext(
            [
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationName, Value = "PS02" },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" }
            ],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters.ShouldNotBeNull().Count.ShouldBe(2);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.StationName).Value.ShouldBe("PS02");
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.StationType).Value.ShouldBe("floor");
    }

    [TestMethod]
    public void Build_WithOptionalParameters_DoesNotAddParameters()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.FinePositioning,
            [
                ActionGenerator.OptionalParameter(Vda5050Specifications.Parameters.StationName),
                ActionGenerator.OptionalParameter(Vda5050Specifications.Parameters.StationType)
            ],
            [BlockingType.SOFT]);
        var context = new OrderActionContext(
            [
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationName, Value = "PS02" },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" }
            ],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters.ShouldNotBeNull().ShouldBeEmpty();
    }
}