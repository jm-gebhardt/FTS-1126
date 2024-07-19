using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Routing.Models;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Action;

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

    public new ActionGroupEstimationTestFixture WithLastActionIsMoveToCoordinate(bool isMoveToCoordinate)
    {
        ActionGroupRepository.Setup(x => x.IsLastCompletedActionMoveToCoordinate(It.IsAny<Guid>())).ReturnsAsync(isMoveToCoordinate);
        return this;
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public ActionGroupEstimationTestFixture AddRouteOnCosts(double onCosts)
    {
        string[] targetNodes = ActionGroup.Actions.Select(a => a.Target.NodeId!).ToArray();
        RouteService.Setup(r => r.GetRouteAsync(Agv, targetNodes.ToArray(), null, It.IsAny<AgvData>()))
            .ReturnsAsync(new Route(onCosts, null!, null!));

        return this;
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public ActionGroupEstimationTestFixture AddDurationEstimateDeviation(double deviation = 0)
    {
        ActionGroupDurationEstimationService.Setup(x => x.GetAverageDurationEstimateDeviationAsync(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deviation);
        return this;
    }

    public ActionGroupEstimationTestFixture AddDuration(double deviation = 0)
    {
        ActionGroupDurationEstimationService.Setup(x => x.GetDurationEstimate(It.IsAny<double>(), It.IsAny<AgvAction>(), It.IsAny<Guid>())).ReturnsAsync(deviation);
        return this;
    }

    public ActionGroup VerifyActionGroupUpdate(Times times)
    {
        ActionGroupRepository.Verify(a => a.UpdateAsync(It.IsAny<ActionGroup>()), times);
        ActionGroup.ShouldNotBeNull();
        return ActionGroup;
    }
}