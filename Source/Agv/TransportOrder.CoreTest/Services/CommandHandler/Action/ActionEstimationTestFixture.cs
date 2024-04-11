using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Action;

public class ActionGroupEstimationTestFixture : ActionGroupTestFixtureBase<ActionCommandHandler>
{
    public TransportOrder.Domain.Models.Agv.Agv Agv { get; }

    public ActionGroup ActionGroup { get; private set; } = null!;

    public ActionGroupEstimationTestFixture(int batteryCharge = 100)
    {
        Agv = AgvGenerator.GenerateIdleAgv(batteryCharge: batteryCharge);
        AddAgv(Agv);
    }

    public override ActionGroupEstimationTestFixture AddActionGroup(ActionGroup actionGroup, bool hasUnfinishedAction = true, bool hasNotStartedAction = true, bool existsPriorToTest = true)
    {
        base.AddActionGroup(actionGroup, hasUnfinishedAction, hasNotStartedAction, existsPriorToTest);
        ActionGroup = actionGroup;

        ActionGroupRepository.Setup(a => a.UpdateAsync(It.IsAny<ActionGroup>()))
            .Callback<ActionGroup>(a => ActionGroup = a);

        return this;
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public ActionGroupEstimationTestFixture AddRouteOnCosts(double onCosts)
    {
        string[] targetNodes = ActionGroup.Actions.Select(a => a.TargetNodeId).ToArray();
        RouteService.Setup(r => r.GetRouteAsync(Agv, targetNodes.ToArray(), null, It.IsAny<AgvData>()))
            .ReturnsAsync(new Route(onCosts, null!));

        return this;
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public ActionGroupEstimationTestFixture AddDurationEstimateDeviation(double deviation = 0)
    {
        ActionGroupDurationEstimationService.Setup(x => x.GetAverageDurationEstimateDeviationAsync(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deviation);
        return this;
    }

    public ActionGroup VerifyActionGroupUpdate(Times times)
    {
        ActionGroupRepository.Verify(a => a.UpdateAsync(It.IsAny<ActionGroup>()), times);
        ActionGroup.ShouldNotBeNull();
        return ActionGroup;
    }
}