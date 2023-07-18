using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions;

[TestClass]
public class ActionManagerTest
{
    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenAgvIsCarryingLoadAndNeedsCharging_StartSubsequentActionToBeUnloadedInNextCycleForCharging()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var subsequentAction = actionGroup.Actions.Last();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddNotStartedActionGroup(actionGroup)
            .AddAssignedActionGroupAndAgv(actionGroup, agv, subsequentAction)
            .SetSubsequentAction(agv, subsequentAction)
            .WithAgvNeedsCharging(agv)
            .WithAgvCarryingLoad(agv);

        await fixture.ActionManager.AssignExclusivelyAsync(agv);
        fixture.VerifyActionStarted(agv, subsequentAction);
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenAgvIsCarryingLoadAndNeedsMaintenance_StartSubsequentActionToWorkOffItsActions()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var subsequentAction = actionGroup.Actions.Last();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddNotStartedActionGroup(actionGroup)
            .AddAssignedActionGroupAndAgv(actionGroup, agv, subsequentAction)
            .SetSubsequentAction(agv, subsequentAction)
            .WithAgvCarryingLoad(agv)
            .WithAgvMaintenanceRequested(agv);

        await fixture.ActionManager.AssignExclusivelyAsync(agv);
        fixture.VerifyActionStarted(agv, subsequentAction);
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenAgvIsCarryingLoadAndCanGetNewLoad_StartSubsequentActionToWorkOffItsActions()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var subsequentAction = actionGroup.Actions.Last();

        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddNotStartedActionGroup(actionGroup)
            .AddAssignedActionGroupAndAgv(actionGroup, agv, subsequentAction)
            .SetSubsequentAction(agv, subsequentAction)
            .WithAgvCarryingLoad(agv)
            .WithAgvCanGetNewLoad(agv)
            .WithAgvHasRouteToDestination(agv);

        await fixture.ActionManager.AssignExclusivelyAsync(agv);
        fixture.VerifyActionStarted(agv, subsequentAction);
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenAgvIsCarryingLoadAndCannotGetNewLoad_StartSubsequentActionToWorkOffItsActions()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var subsequentAction = actionGroup.Actions.Last();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddNotStartedActionGroup(actionGroup)
            .AddAssignedActionGroupAndAgv(actionGroup, agv, subsequentAction)
            .SetSubsequentAction(agv, subsequentAction)
            .WithAgvCarryingLoad(agv);

        await fixture.ActionManager.AssignExclusivelyAsync(agv);
        fixture.VerifyActionStarted(agv, subsequentAction);
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenAgvIsCarryingLoadAndDoesNotNeedChargingOrMaintenance_StartSubsequentActions()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var subsequentAction = actionGroup.Actions.Last();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddNotStartedActionGroup(actionGroup)
            .AddAssignedActionGroupAndAgv(actionGroup, agv, subsequentAction)
            .SetSubsequentAction(agv, subsequentAction)
            .WithAgvCarryingLoad(agv);

        await fixture.ActionManager.AssignExclusivelyAsync(agv);
        fixture.VerifyActionStarted(agv, subsequentAction);
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenAgvIsNotCarryingLoadAndNeedsCharging_StartChargingAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddNotStartedActionGroup(actionGroup)
            .AddAssignedActionGroupAndAgv(actionGroup, agv)
            .AddReservableChargingNode(chargingNode)
            .WithAgvNeedsCharging(agv);

        await fixture.ActionManager.AssignExclusivelyAsync(agv);
        fixture.VerifyActionStarted(a => a == agv, o => o.ActionType == ActionType.Charge && o.TargetNodeId == chargingNode.Id);
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenAgvIsNotCarryingLoadAndCanGetNewLoad_StartNextParentedByAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var firstAction = actionGroup.Actions.First();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddNotStartedActionGroup(actionGroup)
            .AddAssignedActionGroupAndAgv(actionGroup, agv, firstAction)
            .ChooseNextActionGroup(agv, actionGroup)
            .WithAgvHasRouteToDestination(agv)
            .WithAgvCanGetNewLoad(agv);

        await fixture.ActionManager.AssignExclusivelyAsync(agv, cancellationToken: CancellationToken.None);
        fixture.VerifyActionStarted(agv, firstAction);
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenAgvIsNotCarryingLoadAndNeedsMaintenance_StartMaintenanceAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var maintenanceNode = NodeGenerator.GenerateMaintenanceNode();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddNotStartedActionGroup(actionGroup)
            .WithAgvMaintenanceRequested(agv, maintenanceNode: maintenanceNode);

        await fixture.ActionManager.AssignExclusivelyAsync(agv, cancellationToken: CancellationToken.None);
        fixture.VerifyActionStarted(a => a == agv, o => o.ActionType == ActionType.Move && o.TargetNodeId == maintenanceNode.Id);
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenAgvIsEligibleForCharging_StartChargingAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddAssignedActionGroupAndAgv(actionGroup, agv)
            .AddReservableChargingNode(chargingNode)
            .WithAgvEligibleForCharging(agv);

        await fixture.ActionManager.AssignExclusivelyAsync(agv);
        fixture.VerifyActionStarted(a => a == agv, a => a.ActionType == ActionType.Charge && a.TargetNodeId == chargingNode.Id);
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenAgvIsNotEligibleForCharging_StartParkAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var parkingNode = NodeGenerator.GenerateParkingNode();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var parkingAction = AgvActionGenerator.GenerateParkAction(parkingNode.Id);
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddAssignedActionGroupAndAgv(actionGroup, agv, parkingAction)
            .AddReservableParkingNode(parkingNode);

        await fixture.ActionManager.AssignExclusivelyAsync(agv);
        fixture.VerifyActionStarted(a => a == agv, o => o.ActionType == ActionType.Move &&
                                                        o.TargetNodeId == parkingNode.Id);
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenAgvIsEligibleForChargingButIsAlreadyInChargingStatus_StartsNoAction()
    {
        var agv = AgvGenerator.GenerateChargingAgv();
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddReservableChargingNode(chargingNode)
            .WithAgvEligibleForCharging(agv);

        await fixture.ActionManager.AssignExclusivelyAsync(agv);
        fixture.VerifyNoActionStarted();
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenAgvIsAlreadyOnParkNode_StartsNoAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var parkingNode = NodeGenerator.GenerateParkingNode();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddReservableParkingNode(parkingNode)
            .WithAgvOnAParkNode(agv);

        await fixture.ActionManager.AssignExclusivelyAsync(agv);
        fixture.VerifyNoActionStarted();
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenStartingNewAction_LocksAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddNotStartedActionGroup(actionGroup)
            .ChooseNextActionGroup(agv, actionGroup)
            .WithAgvHasRouteToDestination(agv)
            .WithAgvCanGetNewLoad(agv);

        await fixture.ActionManager.AssignExclusivelyAsync(agv, cancellationToken: CancellationToken.None);
        fixture.AgvStateService.Verify(a => a.SetBusyAsync(agv, It.IsAny<bool>()));
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenIgnoreChargingFlagOnAndAgvIsCharging_StartNextParentedByAction()
    {
        var agv = AgvGenerator.GenerateChargingAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var firstAction = actionGroup.Actions.First();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv, readyForLockWhenIgnoreChargingFlag: true)
            .AddNotStartedActionGroup(actionGroup)
            .AddAssignedActionGroupAndAgv(actionGroup, agv, firstAction)
            .ChooseNextActionGroup(agv, actionGroup)
            .WithAgvHasRouteToDestination(agv)
            .WithAgvCanGetNewLoad(agv);

        await fixture.ActionManager.AssignExclusivelyAsync(agv, ignoreChargingFlag: true, cancellationToken: CancellationToken.None);
        fixture.VerifyActionStarted(agv, firstAction);
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenIgnoreChargingFlagOffAndAgvIsCharging_StartsNoAction()
    {
        var agv = AgvGenerator.GenerateChargingAgv();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv, false);

        await fixture.ActionManager.AssignExclusivelyAsync(agv, ignoreChargingFlag: true, cancellationToken: CancellationToken.None);
        fixture.VerifyNoActionStarted();
    }

    [TestMethod]
    public async Task AssignExclusivelyAsync_WhenThereIsNoReadyAgv_StartsNoAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv, false)
            .AddNotStartedActionGroup(actionGroup)
            .WithAgvHasRouteToDestination(agv);

        await fixture.ActionManager.AssignExclusivelyAsync(agv, cancellationToken: CancellationToken.None);
        fixture.VerifyNoActionStarted();
    }

    [TestMethod]
    public async Task CompleteActionAsync_UpdatesActionStateToCompleted()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agvAction = actionGroup.Actions.First();

        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddNotStartedActionGroup(actionGroup)
            .AddAssignedActionGroupAndAgv(actionGroup, agv, agvAction);

        await fixture.ActionManager.CompleteActionAsync(agvAction.CorrelationId, CancellationToken.None);
        fixture.ActionGroupRegistry.Verify(x => x.UpdateActionAndGroupStateAsync(agvAction, agv, ActionState.Completed), Times.Once);
        fixture.Bus.Verify(x => x.Publish(It.Is<ActionGroupChanged>(a => a.ActionGroup.State == ActionGroupState.Created), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CompleteActionAsync_WhenActionTypeIsNotCharge_UnlocksAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agvAction = actionGroup.Actions.First();

        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddNotStartedActionGroup(actionGroup)
            .AddAssignedActionGroupAndAgv(actionGroup, agv, agvAction);

        await fixture.ActionManager.CompleteActionAsync(agvAction.CorrelationId, CancellationToken.None);
        fixture.AgvStateService.Verify(x => x.SetIdleAsync(agv), Times.Once);
    }

    [TestMethod]
    public async Task CompleteActionAsync_WhenActionTypeIsCharge_DoesNotUnlockAgv()
    {
        //unlock is not needed because the AgvStateMachine switches to Charging state on it's own, therefore no need to manually set to idle state
        var agv = AgvGenerator.GenerateIdleAgv();
        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var chargeAction = chargeActionGroup.Actions.First();

        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddAssignedActionGroupAndAgv(chargeActionGroup, agv, chargeAction);

        await fixture.ActionManager.CompleteActionAsync(chargeAction.CorrelationId, CancellationToken.None);
        fixture.AgvStateService.Verify(x => x.SetIdleAsync(agv), Times.Never);
    }

    [TestMethod]
    public async Task AddActionGroupAsync_ShouldAddAgvActionToRegistry()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var fixture = new ActionManagerFixture();

        await fixture.ActionManager.AddActionGroupAsync(actionGroup, CancellationToken.None);

        fixture.ActionGroupRegistry.Verify(x => x.AddAsync(actionGroup), Times.Once);
    }

    [TestMethod]
    public async Task AddActionGroupAsync_IfNoAgvWasChosen_StartNoActions()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv);

        await fixture.ActionManager.AddActionGroupAsync(actionGroup, CancellationToken.None);
        fixture.VerifyNoActionStarted();
    }

    [TestMethod]
    public async Task AddActionGroupAsync_IfWasChosen_StartsActionForChosenAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var subsequentAction = actionGroup.Actions.First();

        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv)
            .AddNotStartedActionGroup(actionGroup)
            .AddAssignedActionGroupAndAgv(actionGroup, agv, subsequentAction)
            .ChooseNextActionGroup(agv, actionGroup)
            .WithAgvHasRouteToDestination(agv)
            .ChooseNextAgv(agv, actionGroup);

        await fixture.ActionManager.AddActionGroupAsync(actionGroup, CancellationToken.None);
        fixture.VerifyActionStarted(agv, subsequentAction);
    }

    [TestMethod]
    public async Task AddActionGroupAsync_WhenThereIsNoReadyAgv_StartsNoAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv, false)
            .AddNotStartedActionGroup(actionGroup)
            .WithAgvHasRouteToDestination(agv);

        await fixture.ActionManager.AddActionGroupAsync(actionGroup, CancellationToken.None);
        fixture.VerifyNoActionStarted();
    }

    [TestMethod]
    public async Task AddActionGroupAsync_IfNoAgvWasChosen_StartsNoAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv, false)
            .AddNotStartedActionGroup(actionGroup)
            .WithAgvHasRouteToDestination(agv)
            .ChooseNextAgv(null, actionGroup);

        await fixture.ActionManager.AddActionGroupAsync(actionGroup, CancellationToken.None);
        fixture.VerifyNoActionStarted();
    }

    [TestMethod]
    public async Task AddActionGroupAsync_WhenThereAreMultipleReadyAgv_StartsActionInChosenAgv()
    {
        var agv0 = AgvGenerator.GenerateIdleAgv(index: 0);
        var agv1 = AgvGenerator.GenerateIdleAgv(index: 1);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var firstAction = actionGroup.Actions.First();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv0)
            .AddReadyAgv(agv1)
            .AddNotStartedActionGroup(actionGroup)
            .AddAssignedActionGroupAndAgv(actionGroup, agv0, firstAction)
            .ChooseNextActionGroup(agv0, actionGroup)
            .WithAgvHasRouteToDestination(agv0)
            .ChooseNextAgv(agv0, actionGroup);

        await fixture.ActionManager.AddActionGroupAsync(actionGroup, CancellationToken.None);
        fixture.VerifyActionStarted(a => a == agv0, a => a == firstAction);
    }

    [TestMethod]
    public async Task AddActionGroupAsync_WhenThereAreReadyAndNotReadyAgv_StartsActionOfReadyAgv()
    {
        var agv0 = AgvGenerator.GenerateIdleAgv(index: 0);
        var agv1 = AgvGenerator.GenerateIdleAgv(index: 1);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        actionGroup.Actions.ShouldNotBeNull();
        var firstAction = actionGroup.Actions.First();
        var fixture = new ActionManagerFixture()
            .AddReadyAgv(agv0, false)
            .AddReadyAgv(agv1)
            .AddNotStartedActionGroup(actionGroup)
            .AddAssignedActionGroupAndAgv(actionGroup, agv1, firstAction)
            .ChooseNextActionGroup(agv1, actionGroup)
            .WithAgvHasRouteToDestination(agv1)
            .ChooseNextAgv(agv1, actionGroup);

        await fixture.ActionManager.AddActionGroupAsync(actionGroup, CancellationToken.None);
        fixture.VerifyActionStarted(a => a == agv1, a => a == firstAction);
    }
}