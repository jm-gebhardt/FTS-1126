using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

[TestClass]
public class WaitForTriggerOrderActionBuilderTest
{
    private readonly WaitForTriggerOrderActionBuilder _builder = new();

    [TestMethod]
    public void ActionType_ReturnsWaitForTriggerConstant()
    {
        _builder.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.WaitForTrigger);
    }

    [TestMethod]
    public void Build_NoActionParameters_ThrowsRequiredAgvActionParameterMissing()
    {
        var agvAction = new AgvAction { ActionType = Vda5050Specifications.ActionTypes.WaitForTrigger };
        var context = new OrderActionContext([], BlockingType.HARD, true);

        Should.Throw<RequiredAgvActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_TriggerTypeMissingFromContext_ThrowsRequiredOrderActionParameterMissing()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.WaitForTrigger,
            ActionParameters = [new AgvActionParameter { Key = Vda5050Specifications.Parameters.TriggerType, IsOptional = false }]
        };
        var context = new OrderActionContext([], BlockingType.HARD, true);

        Should.Throw<RequiredOrderActionParameterMissing>(() => _builder.Build(agvAction, context));
    }

    [TestMethod]
    public void Build_ValidParameters_ReturnsActionWithTriggerType()
    {
        var agvAction = new AgvAction
        {
            ActionType = Vda5050Specifications.ActionTypes.WaitForTrigger,
            ActionParameters = [new AgvActionParameter { Key = Vda5050Specifications.Parameters.TriggerType, IsOptional = false }],
            BlockingTypes = [BlockingType.SOFT]
        };
        var context = new OrderActionContext(
            [new OrderActionParameter { Key = Vda5050Specifications.Parameters.TriggerType, Value = "doorOpen" }],
            BlockingType.SOFT,
            true);

        var result = _builder.Build(agvAction, context);

        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.WaitForTrigger);
        result.ActionParameters!.Count.ShouldBe(1);
        result.ActionParameters.Single(p => p.Key == Vda5050Specifications.Parameters.TriggerType).Value.ShouldBe("doorOpen");
    }
}
