using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

[TestClass]
public class InitPositionOrderActionBuilderTest
{
    private readonly InitPositionOrderActionBuilder _builder = new();

    [TestMethod]
    public void ActionType_ReturnsInitPositionConstant()
    {
        _builder.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.InitPosition);
    }

    [TestMethod]
    public void Build_NoActionParameters_ThrowsRequiredAgvActionParameterMissing()
    {
        var agvAction = new AgvAction { ActionType = Vda5050Specifications.ActionTypes.InitPosition };
        var context = new OrderActionContext([], BlockingType.HARD, true);

        Should.Throw<RequiredAgvActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_PositionMissingFromContext_ThrowsRequiredOrderActionParameterMissing()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.InitPosition,
            ActionParameters = [new AgvActionParameter { Key = Vda5050Specifications.Parameters.X, IsOptional = false }]
        };
        var context = new OrderActionContext([], BlockingType.HARD, true);

        Should.Throw<RequiredOrderActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_LastNodeIdMissingFromContext_ThrowsRequiredOrderActionParameterMissing()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.InitPosition,
            ActionParameters = [new AgvActionParameter { Key = Vda5050Specifications.Parameters.X, IsOptional = false }]
        };
        var context = new OrderActionContext(
            [
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.X, Value = 1.0f },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.Y, Value = 2.0f },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.Theta, Value = 0.5f }
            ],
            BlockingType.HARD,
            true);

        Should.Throw<RequiredOrderActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_MapIdMissingFromContext_ThrowsRequiredOrderActionParameterMissing()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.InitPosition,
            ActionParameters = [new AgvActionParameter { Key = Vda5050Specifications.Parameters.X, IsOptional = false }]
        };
        var context = new OrderActionContext(
            [
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.X, Value = 1.0f },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.Y, Value = 2.0f },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.Theta, Value = 0.5f },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.LastNodeId, Value = "node-1" }
            ],
            BlockingType.HARD,
            true);

        Should.Throw<RequiredOrderActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_ValidParameters_ReturnsActionWithAllPositionParameters()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.InitPosition,
            ActionParameters = [new AgvActionParameter { Key = Vda5050Specifications.Parameters.X, IsOptional = false }],
            BlockingTypes = [BlockingType.HARD]
        };
        var context = new OrderActionContext(
            [
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.X, Value = 1.5f },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.Y, Value = 2.5f },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.Theta, Value = 0.75f },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.LastNodeId, Value = "node-42" },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.MapId, Value = "map-1" }
            ],
            BlockingType.HARD,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.InitPosition);
        result.ActionParameters!.Count.ShouldBe(5);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.X).Value.ShouldBe(1.5f);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.Y).Value.ShouldBe(2.5f);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.Theta).Value.ShouldBe(0.75f);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.LastNodeId).Value.ShouldBe("node-42");
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.MapId).Value.ShouldBe("map-1");
    }
}
