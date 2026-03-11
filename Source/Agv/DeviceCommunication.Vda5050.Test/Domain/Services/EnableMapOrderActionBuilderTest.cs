using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

[TestClass]
public class EnableMapOrderActionBuilderTest
{
    private readonly EnableMapOrderActionBuilder _builder = new();

    [TestMethod]
    public void ActionType_ReturnsEnableMapConstant()
    {
        _builder.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.EnableMap);
    }

    [TestMethod]
    public void Build_NoActionParameters_ThrowsRequiredAgvActionParameterMissing()
    {
        var agvAction = new AgvAction { ActionType = Vda5050Specifications.ActionTypes.EnableMap };
        var context = new OrderActionContext([], BlockingType.HARD, true);

        Should.Throw<RequiredAgvActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_MapIdMissingFromContext_ThrowsRequiredOrderActionParameterMissing()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.EnableMap,
            ActionParameters = [new AgvActionParameter { Key = Vda5050Specifications.Parameters.MapId, IsOptional = false }]
        };
        var context = new OrderActionContext([], BlockingType.HARD, true);

        Should.Throw<RequiredOrderActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_MapVersionMissingFromContext_ThrowsRequiredOrderActionParameterMissing()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.EnableMap,
            ActionParameters =
            [
                new AgvActionParameter { Key = Vda5050Specifications.Parameters.MapId, IsOptional = false },
                new AgvActionParameter { Key = Vda5050Specifications.Parameters.MapVersion, IsOptional = false }
            ]
        };
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.MapId, Value = "map-1" }],
            BlockingType.HARD,
            true);

        Should.Throw<RequiredOrderActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_ValidParameters_ReturnsActionWithMapIdAndMapVersion()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.EnableMap,
            ActionParameters =
            [
                new AgvActionParameter { Key = Vda5050Specifications.Parameters.MapId, IsOptional = false },
                new AgvActionParameter { Key = Vda5050Specifications.Parameters.MapVersion, IsOptional = false }
            ],
            BlockingTypes = [BlockingType.HARD]
        };
        var context = new OrderActionContext(
            [
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.MapId, Value = "map-1" },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.MapVersion, Value = "1.2.3" }
            ],
            BlockingType.HARD,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.EnableMap);
        result.ActionParameters!.Count.ShouldBe(2);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.MapId).Value.ShouldBe("map-1");
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.MapVersion).Value.ShouldBe("1.2.3");
    }
}
