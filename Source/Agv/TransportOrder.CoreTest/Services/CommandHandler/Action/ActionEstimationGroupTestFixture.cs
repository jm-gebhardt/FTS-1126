using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Action;

public class ActionEstimationGroupTestFixture : ActionGroupTestFixtureBase<ActionEstimationCommandHandler>
{
    public TransportOrder.Domain.Models.Agv.Agv Agv { get; }

    public ActionGroup ActionGroup { get; private set; } = null!;

    public ActionEstimationGroupTestFixture(int batteryCharge = 100)
    {
        Agv = AgvGenerator.GenerateIdleAgv(batteryCharge: batteryCharge);
        AddAgv(Agv);
    }

    public override ActionEstimationGroupTestFixture AddActionGroup(ActionGroup actionGroup, bool hasUnfinishedAction = true, bool hasNotStartedAction = true, bool existsPriorToTest = true)
    {
        base.AddActionGroup(actionGroup, hasUnfinishedAction, hasNotStartedAction, existsPriorToTest);
        ActionGroup = actionGroup;

        ActionGroupRepository.Setup(a => a.UpdateAsync(It.IsAny<ActionGroup>()))
            .Callback<ActionGroup>(a => ActionGroup = a);

        return this;
    }

    public ActionEstimationGroupTestFixture AddRouteOnCosts(double onCosts)
    {
        string[] targetNodes = ActionGroup.Actions.Select(a => a.TargetNodeId).ToArray();
        RouteService.Setup(r => r.GetRouteAsync(Agv, targetNodes.Skip(1).ToArray(), targetNodes[0], It.IsAny<AgvData>()))
            .ReturnsAsync(new Route(onCosts, null!));

        return this;
    }

    public ActionGroup GetUpdatedActionGroup()
    {
        ActionGroupRepository.Verify(a => a.UpdateAsync(It.IsAny<ActionGroup>()), Times.Once);
        ActionGroup.ShouldNotBeNull();
        return ActionGroup;
    }
}