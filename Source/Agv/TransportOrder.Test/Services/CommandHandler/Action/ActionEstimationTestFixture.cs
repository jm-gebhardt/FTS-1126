using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler.ActionGroupHandlers;
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

    public override ActionGroupEstimationTestFixture AddActionGroup(ActionGroup actionGroup, bool hasUnfinishedAction = true, bool hasNotStartedAction = true, bool existsPriorToTest = true, bool isManual = false)
    {
        base.AddActionGroup(actionGroup, hasUnfinishedAction, hasNotStartedAction, existsPriorToTest, isManual);
        ActionGroup = actionGroup;

        ActionGroupRepository.Setup(a => a.UpdateAsync(It.IsAny<ActionGroup>(), It.IsAny<CancellationToken>()))
            .Callback<ActionGroup, CancellationToken>((a, _) => ActionGroup = a);

        return this;
    }

    public new ActionGroupEstimationTestFixture WithLastActionIsMoveToCoordinate(bool isMoveToCoordinate)
    {
        base.WithLastActionIsMoveToCoordinate(isMoveToCoordinate);
        return this;
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public ActionGroupEstimationTestFixture AddRouteOnCosts(double onCosts)
    {
        RouteService.Setup(r => r.GetRouteAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<Guid[]>(), null, It.IsAny<AgvData>()))
            .ReturnsAsync(new Route(onCosts, [], []));

        return this;
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public ActionGroupEstimationTestFixture AddNonRouteCosts(double cost = 0)
    {
        ActionGroupDurationEstimationService.Setup(x => x.AddNonRouteCosts(It.IsAny<bool>(), It.IsAny<ActionGroup>(), It.IsAny<AgvAction>(), It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).ReturnsAsync(cost);
        return this;
    }

    public ActionGroupEstimationTestFixture AddDuration(double deviation = 0)
    {
        ActionGroupDurationEstimationService.Setup(x => x.GetDurationEstimate(It.IsAny<double>(), It.IsAny<AgvAction>(), It.IsAny<Guid>())).ReturnsAsync(deviation);
        return this;
    }

    public ActionGroup VerifyActionGroupUpdate(Times times)
    {
        ActionGroupRepository.Verify(a => a.UpdateAsync(It.IsAny<ActionGroup>(), It.IsAny<CancellationToken>()), times);
        ActionGroup.ShouldNotBeNull();
        return ActionGroup;
    }
}