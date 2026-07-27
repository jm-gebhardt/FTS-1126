using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
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
        var agvAction = ActionGenerator.CreateAction(Vda5050Specifications.ActionTypes.Drop);
        var context = new OrderActionContext([], BlockingType.SOFT, true);

        Should.Throw<RequiredAgvActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_EmptyActionParameters_ThrowsRequiredAgvActionParameterMissing()
    {
        var agvAction = ActionGenerator.CreateAction(Vda5050Specifications.ActionTypes.Drop, []);
        var context = new OrderActionContext([], BlockingType.SOFT, true);

        Should.Throw<RequiredAgvActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_StationTypeMissingFromContext_ThrowsRequiredOrderActionParameterMissing()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.Drop,
            [ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationType)]);
        var context = new OrderActionContext([], BlockingType.SOFT, true);

        Should.Throw<RequiredOrderActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_StationNameRequiredButMissingFromContext_ThrowsRequiredOrderActionParameterMissing()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.Drop,
            [
                ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationType),
                ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationName)
            ]);
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" }],
            BlockingType.SOFT,
            true);

        Should.Throw<RequiredOrderActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_LoadHandlingDeviceRequiredButMissingFromContext_ThrowsRequiredOrderActionParameterMissing()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.Drop,
            [
                ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationType),
                ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.LoadHandlingDevice)
            ]);
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" }],
            BlockingType.SOFT,
            true);

        Should.Throw<RequiredOrderActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_ValidWithOnlyStationType_ReturnsActionWithStationType()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.Drop,
            [ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationType)],
            [BlockingType.SOFT]);
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" }],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.Drop);
        result.ActionParameters.ShouldNotBeNull().Count.ShouldBe(1);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.StationType).Value.ShouldBe("floor");
    }

    [TestMethod]
    public void Build_WithRequiredStationName_AddsStationNameParameter()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.Drop,
            [
                ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationType),
                ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationName)
            ],
            [BlockingType.SOFT]);
        var context = new OrderActionContext(
            [
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationName, Value = "DS01" }
            ],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters.ShouldNotBeNull().Count.ShouldBe(2);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.StationName).Value.ShouldBe("DS01");
    }

    [TestMethod]
    public void Build_WithOptionalStationName_DoesNotAddStationNameParameter()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.Drop,
            [
                ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationType),
                ActionGenerator.OptionalParameter(Vda5050Specifications.Parameters.StationName)
            ],
            [BlockingType.SOFT]);
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" }],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters.ShouldNotBeNull().Count.ShouldBe(1);
        result.ActionParameters.ShouldNotContain(p => p.Key == Vda5050Specifications.Parameters.StationName);
    }

    [TestMethod]
    public void Build_WithRequiredLoadHandlingDevice_AddsLoadHandlingDeviceParameter()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.Drop,
            [
                ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationType),
                ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.LoadHandlingDevice)
            ],
            [BlockingType.SOFT]);
        var context = new OrderActionContext(
            [
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.LoadHandlingDevice, Value = "LHD2" }
            ],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters.ShouldNotBeNull().Count.ShouldBe(2);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.LoadHandlingDevice).Value.ShouldBe("LHD2");
    }

    [TestMethod]
    public void Build_WithRequiredLoadType_AddsLoadTypeParameter()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.Drop,
            [
                ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationType),
                ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.LoadType)
            ],
            [BlockingType.SOFT]);
        var context = new OrderActionContext(
            [
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" },
                new OrderActionParameter { Key = Vda5050Specifications.Parameters.LoadType, Value = "pallet" }
            ],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters.ShouldNotBeNull().Count.ShouldBe(2);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.LoadType).Value.ShouldBe("pallet");
    }

    [TestMethod]
    public void Build_WithOptionalLoadType_DoesNotAddLoadTypeParameter()
    {
        var agvAction = ActionGenerator.CreateAction(
            Vda5050Specifications.ActionTypes.Drop,
            [
                ActionGenerator.RequiredParameter(Vda5050Specifications.Parameters.StationType),
                ActionGenerator.OptionalParameter(Vda5050Specifications.Parameters.LoadType)
            ],
            [BlockingType.SOFT]);
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "floor" }],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionParameters.ShouldNotBeNull().Count.ShouldBe(1);
        result.ActionParameters.ShouldNotContain(p => p.Key == Vda5050Specifications.Parameters.LoadType);
    }
}