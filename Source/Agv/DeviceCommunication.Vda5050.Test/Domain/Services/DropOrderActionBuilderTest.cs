using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

[TestClass]
public class DropOrderActionBuilderTest
{
    private readonly DropOrderActionBuilder _builder = new();

    [TestMethod]
    public void ActionType_ReturnsDropConstant()
    {
        _builder.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.Drop);
    }

    [TestMethod]
    public void Build_NoActionParameters_ThrowsRequiredAgvActionParameterMissing()
    {
        var agvAction = new AgvAction { ActionType = Vda5050Specifications.ActionTypes.Drop };
        var context = new OrderActionContext([], BlockingType.SOFT, true);

        Should.Throw<RequiredAgvActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_EmptyActionParameters_ThrowsRequiredAgvActionParameterMissing()
    {
        var agvAction = new AgvAction { ActionType = Vda5050Specifications.ActionTypes.Drop, ActionParameters = [] };
        var context = new OrderActionContext([], BlockingType.SOFT, true);

        Should.Throw<RequiredAgvActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_StationTypeMissingFromContext_ThrowsRequiredOrderActionParameterMissing()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.Drop,
            ActionParameters = [new AgvActionParameter { Key = Vda5050Specifications.Parameters.StationType, IsOptional = false }]
        };
        var context = new OrderActionContext([], BlockingType.SOFT, true);

        Should.Throw<RequiredOrderActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_StationNameRequiredButMissingFromContext_ThrowsRequiredOrderActionParameterMissing()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.Drop,
            ActionParameters =
            [
                new AgvActionParameter { Key = Vda5050Specifications.Parameters.StationType, IsOptional = false },
                new AgvActionParameter { Key = Vda5050Specifications.Parameters.StationName, IsOptional = false }
            ]
        };
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" }],
            BlockingType.SOFT,
            true);

        Should.Throw<RequiredOrderActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_LoadHandlingDeviceRequiredButMissingFromContext_ThrowsRequiredOrderActionParameterMissing()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.Drop,
            ActionParameters =
            [
                new AgvActionParameter { Key = Vda5050Specifications.Parameters.StationType, IsOptional = false },
                new AgvActionParameter { Key = Vda5050Specifications.Parameters.LoadHandlingDevice, IsOptional = false }
            ]
        };
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" }],
            BlockingType.SOFT,
            true);

        Should.Throw<RequiredOrderActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_ValidWithOnlyStationType_ReturnsActionWithStationType()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.Drop,
            ActionParameters = [new AgvActionParameter { Key = Vda5050Specifications.Parameters.StationType, IsOptional = false }],
            BlockingTypes = [BlockingType.SOFT]
        };
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" }],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.Drop);
        result.ActionParameters!.Count.ShouldBe(1);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.StationType).Value.ShouldBe("floor");
    }

    [TestMethod]
    public void Build_WithRequiredStationName_AddsStationNameParameter()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.Drop,
            ActionParameters =
            [
                new AgvActionParameter { Key = Vda5050Specifications.Parameters.StationType, IsOptional = false },
                new AgvActionParameter { Key = Vda5050Specifications.Parameters.StationName, IsOptional = false }
            ],
            BlockingTypes = [BlockingType.SOFT]
        };
        var context = new OrderActionContext(
            [
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationName, Value = "DS01" }
            ],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters!.Count.ShouldBe(2);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.StationName).Value.ShouldBe("DS01");
    }

    [TestMethod]
    public void Build_WithOptionalStationName_DoesNotAddStationNameParameter()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.Drop,
            ActionParameters =
            [
                new AgvActionParameter { Key = Vda5050Specifications.Parameters.StationType, IsOptional = false },
                new AgvActionParameter { Key = Vda5050Specifications.Parameters.StationName, IsOptional = true }
            ],
            BlockingTypes = [BlockingType.SOFT]
        };
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" }],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters!.Count.ShouldBe(1);
        result.ActionParameters.ShouldNotContain(p => p.Key == Vda5050Specifications.Parameters.StationName);
    }

    [TestMethod]
    public void Build_WithRequiredLoadHandlingDevice_AddsLoadHandlingDeviceParameter()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.Drop,
            ActionParameters =
            [
                new AgvActionParameter { Key = Vda5050Specifications.Parameters.StationType, IsOptional = false },
                new AgvActionParameter { Key = Vda5050Specifications.Parameters.LoadHandlingDevice, IsOptional = false }
            ],
            BlockingTypes = [BlockingType.SOFT]
        };
        var context = new OrderActionContext(
            [
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.LoadHandlingDevice, Value = "LHD2" }
            ],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters!.Count.ShouldBe(2);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.LoadHandlingDevice).Value.ShouldBe("LHD2");
    }
}
