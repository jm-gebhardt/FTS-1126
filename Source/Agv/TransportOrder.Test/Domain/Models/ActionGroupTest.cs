using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Extensions;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Globalization;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class ActionGroupTests
{
    [TestMethod]
    [DataRow(ActionState.Created, ActionState.Created, ActionGroupState.Created)]
    [DataRow(ActionState.Started, ActionState.Created, ActionGroupState.Started)]
    [DataRow(ActionState.Completed, ActionState.Created, ActionGroupState.Started)]
    [DataRow(ActionState.Completed, ActionState.Started, ActionGroupState.Started)]
    [DataRow(ActionState.Completed, ActionState.Completed, ActionGroupState.Completed)]
    [DataRow(ActionState.Invalid, ActionState.Created, ActionGroupState.Invalid)]
    [DataRow(ActionState.Canceled, ActionState.Created, ActionGroupState.Canceled)]
    public void VerifyAllStateTransition_ForActionGroupWithTwoActions(ActionState actionState1, ActionState actionState2, ActionGroupState actionGroupState)
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        actionGroup.Actions.ElementAt(0).UpdateActionState(actionState1);
        actionGroup.Actions.ElementAt(1).UpdateActionState(actionState2);
        actionGroup.UpdateActionGroupState();
        actionGroup.State.ShouldBe(actionGroupState);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenAllActionsInStateCreated_SetActionGroupStateToCreated()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        actionGroup.UpdateActionGroupState();

        actionGroup.State.ShouldBe(ActionGroupState.Created);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenActionGroupStateIsStartedAndDifferentFromPreviousState_UpdateActionGroupStateToStartedAndSetStartedDate()
    {
        var now = DateTime.UtcNow;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        actionGroup.StartedDate.ShouldBeNull();
        string[] nodes = ["source", "target"];
        Guid[] edges = [Guid.NewGuid(), Guid.NewGuid()];

        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, nodes, edges);
        actionGroup.UpdateActionGroupState();

        actionGroup.State.ShouldBe(ActionGroupState.Started);
        actionGroup.StartedDate.ShouldNotBeNull();
        actionGroup.StartedDate.Value.ShouldBeInRange(now, DateTime.UtcNow);
        actionGroup.Actions[0].Nodes.ShouldBe(nodes);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenActionGroupStateIsStartedAndSameFromPreviousState_UpdateActionGroupStateToStartedAndDoesNotSetStartedDate()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var previousDate = DateTime.Today.AddDays(-10);

        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, [], []);
        actionGroup.StartedDate = previousDate;

        actionGroup.UpdateActionGroupState();

        actionGroup.State.ShouldBe(ActionGroupState.Started);
        actionGroup.StartedDate.Value.ShouldBe(previousDate);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenActionGroupStateIsCompletedAndDifferentFromPreviousState_UpdateActionGroupStateToCompletedAndSetCompletedDate()
    {
        var now = DateTime.UtcNow;

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        actionGroup.SetActionState(ActionState.Completed);
        actionGroup.CompletedDate.ShouldBeNull();

        actionGroup.UpdateActionGroupState();

        actionGroup.State.ShouldBe(ActionGroupState.Completed);
        actionGroup.CompletedDate.ShouldNotBeNull();
        actionGroup.CompletedDate.Value.ShouldBeInRange(now, DateTime.UtcNow);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenActionGroupStateIsCompletedAndSameFromPreviousState_UpdateActionGroupStateToCompletedAndDoesNotSetCompletedDate()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        const ActionGroupState actionGroupState = ActionGroupState.Completed;
        var previousDate = DateTime.Today.AddDays(-10);
        actionGroup.SetActionState(ActionState.Completed);

        actionGroup.UpdateActionGroupState();

        actionGroup.CompletedDate = previousDate;

        actionGroup.UpdateActionGroupState();

        actionGroup.State.ShouldBe(actionGroupState);
        actionGroup.CompletedDate.ShouldNotBeNull();
        actionGroup.CompletedDate.Value.Date.ShouldBe(previousDate);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenOneActionFailedOrCanceled_ActionGroupFailsOrIsCanceled()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var now = DateTime.UtcNow;

        actionGroup.SetActionFailed(actionGroup.Actions.First().CorrelationId);

        actionGroup.State.ShouldBe(ActionGroupState.Failed);
        actionGroup.CompletedDate.ShouldNotBeNull();
        actionGroup.CompletedDate.Value.ShouldBeInRange(now, DateTime.UtcNow);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenOneActionCanceled_ActionGroupIsCanceled()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var now = DateTime.UtcNow;

        actionGroup.CancelAction(actionGroup.Actions.First().CorrelationId);

        actionGroup.State.ShouldBe(ActionGroupState.Canceled);
        actionGroup.CompletedDate.ShouldNotBeNull();
        actionGroup.CompletedDate.Value.ShouldBeInRange(now, DateTime.UtcNow);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenOneActionInvalid_ActionGroupInvalid()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var now = DateTime.UtcNow;

        actionGroup.InvalidateAction(actionGroup.Actions.First().CorrelationId);

        actionGroup.State.ShouldBe(ActionGroupState.Invalid);
        actionGroup.CompletedDate.ShouldNotBeNull();
        ((DateTime)actionGroup.CompletedDate).ShouldBeInRange(now, DateTime.UtcNow);
    }

    [TestMethod]
    public void AssignAgvToActionGroup_WhenActionGroupAssignedAgvIsNull_SetActionGroupAssignedAgv()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agvToAssign = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = null;

        actionGroup.Assign(agvToAssign);

        actionGroup.AssignedAgv.ShouldNotBeNull();
        actionGroup.AssignedAgv.ShouldBe(agvToAssign.CorrelationId);
    }

    [TestMethod]
    public void AssignAgvToActionGroup_WhenActionGroupAssignedAgvIsNotNullAndDifferentFromAgvToAssign_ThrowActionGroupAlreadyAssignedException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionGroupAssignedAgv = Guid.NewGuid();
        var agvToAssign = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = actionGroupAssignedAgv;

        Should.Throw<ActionGroupAlreadyAssignedException>(() => actionGroup.Assign(agvToAssign));
    }

    [TestMethod]
    public void CompleteAction_LastActionToBeCompleted_SetActionGroupStateCompletedAndAssignedAgvIsSet()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, assignedAgv: Guid.NewGuid());
        actionGroup.CompleteAction(actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId);
        actionGroup.State.ShouldBe(ActionGroupState.Completed);
        actionGroup.AssignedAgv.ShouldNotBeNull();
    }

    [TestMethod]
    public void ResetActionGroup_ValidData_DatesAndStatesReset()
    {
        var createdDate = DateTime.UtcNow;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Started,
            assignedAgv: Guid.NewGuid(),
            createdDate: createdDate);
        actionGroup.ResetActionGroup();

        actionGroup.AssignedAgv.ShouldBeNull();
        actionGroup.StartedDate.ShouldBeNull();
        actionGroup.CompletedDate.ShouldBeNull();
        actionGroup.EstimatedDuration.ShouldBe(TimeSpan.Zero);
        actionGroup.CreatedDate.ShouldBe(createdDate);
        actionGroup.State.ShouldBe(ActionGroupState.Created);
        actionGroup.Actions.ShouldAllBe(a => a.Nodes.Length == 0);
        actionGroup.Actions.ShouldAllBe(a => a.State == ActionState.Created);
    }

    [TestMethod]
    public void StartAction_ValidData_NodesAndDatesSet()
    {
        var now = DateTime.UtcNow;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        actionGroup.StartedDate.ShouldBeNull();
        string[] nodes = ["source", "target"];
        Guid[] edges = [Guid.NewGuid(), Guid.NewGuid()];
        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, nodes, edges);

        actionGroup.State.ShouldBe(ActionGroupState.Started);
        actionGroup.StartedDate.ShouldNotBeNull();
        actionGroup.StartedDate.Value.ShouldBeInRange(now, DateTime.UtcNow);
        actionGroup.Actions[0].Nodes.ShouldBe(nodes);
    }

    [TestMethod]
    public void GetTargets_PickDropActionGroup_TargetNodeIdsOfPickAndDropAction()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        string[] expectedTargets = [actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).Target.NodeId!, actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).Target.NodeId!];

        actionGroup.GetTargets().ShouldBe(expectedTargets);
    }

    [TestMethod]
    public void GetTargets_IncompletePickDropActionGroupWithoutDropTarget_TargetNodeIdsOfPickAndMoveAction()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget();
        string[] expectedTargets = [actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).Target.NodeId!, actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.NodeId!];

        actionGroup.GetTargets().ShouldBe(expectedTargets);
    }

    [TestMethod]
    public void GetTargets_IncompletePickDropActionGroupWithoutPickTarget_TargetNodeIdOfMoveAction()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget();
        string[] expectedTargets = [actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.NodeId!];

        actionGroup.GetTargets().ShouldBe(expectedTargets);
    }

    [TestMethod]
    public void SetTargetForAction_ValidData_TargetOfIncompleteActionSet()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget();
        var action = actionGroup.Actions.First(a => a.IsIncomplete);
        const string target = "target";

        actionGroup.SetTargetForAction(action.CorrelationId, target);

        actionGroup.IsIncomplete.ShouldBeFalse();
        action.Target.NodeId.ShouldBe(target);
        action.IsIncomplete.ShouldBeFalse();
    }

    [TestMethod]
    public void SetTargetForAction_UnknownAction_ThrowsActionNotFoundException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        Should.Throw<ActionNotFoundException>(() => actionGroup.SetTargetForAction("unknown".ToGuid(), "target"));
    }

    [TestMethod]
    public void SetTargetForAction_ActionNotIncomplete_ThrowsSetActionTargetFailedException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var action = actionGroup.Actions.First(a => !a.IsIncomplete);

        Should.Throw<SetActionTargetFailedException>(() => actionGroup.SetTargetForAction(action.CorrelationId, "target"));
    }

    [TestMethod]
    public void PauseActionGroup_MaintenanceReason_StartedActionPaused()
    {
        var agvGuid = Guid.NewGuid();
        var createdDate = DateTime.UtcNow;

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Started,
            assignedAgv: agvGuid,
            createdDate: createdDate);
        actionGroup.PauseActionGroup(PauseReason.MaintenanceMode.ToReason());

        actionGroup.AssignedAgv.ShouldBe(agvGuid);
        actionGroup.StartedDate.ShouldNotBeNull();
        actionGroup.CompletedDate.ShouldBeNull();
        actionGroup.CreatedDate.ShouldBe(createdDate);
        actionGroup.State.ShouldBe(ActionGroupState.Paused);
        var pausedAction = actionGroup.Actions.FirstOrDefault(a => a.State == ActionState.Paused);
        pausedAction.ShouldNotBeNull();
        actionGroup.Reasons.Count.ShouldBe(1);
    }

    [TestMethod]
    public void PauseActionGroup_LockStationReason_StartedActionPaused()
    {
        var agvGuid = Guid.NewGuid();
        var createdDate = DateTime.UtcNow;

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Started,
            assignedAgv: agvGuid,
            createdDate: createdDate);
        actionGroup.PauseActionGroup(PauseReason.LockedStation.ToReason());

        actionGroup.AssignedAgv.ShouldBe(agvGuid);
        actionGroup.StartedDate.ShouldNotBeNull();
        actionGroup.CompletedDate.ShouldBeNull();
        actionGroup.CreatedDate.ShouldBe(createdDate);
        actionGroup.State.ShouldBe(ActionGroupState.Paused);
        var pausedAction = actionGroup.Actions.FirstOrDefault(a => a.State == ActionState.Paused);
        pausedAction.ShouldNotBeNull();
        actionGroup.Reasons.Count.ShouldBe(1);
    }

    [TestMethod]
    public void PauseActionGroup_LockStationAndMaintenanceReason_StartedActionPaused_TwoReasons()
    {
        var agvGuid = Guid.NewGuid();
        var createdDate = DateTime.UtcNow;

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Started,
            assignedAgv: agvGuid,
            createdDate: createdDate);
        actionGroup.PauseActionGroup(PauseReason.LockedStation.ToReason());

        actionGroup.PauseActionGroup(PauseReason.MaintenanceMode.ToReason());

        actionGroup.AssignedAgv.ShouldBe(agvGuid);
        actionGroup.StartedDate.ShouldNotBeNull();
        actionGroup.CompletedDate.ShouldBeNull();
        actionGroup.CreatedDate.ShouldBe(createdDate);
        actionGroup.State.ShouldBe(ActionGroupState.Paused);
        var pausedAction = actionGroup.Actions.FirstOrDefault(a => a.State == ActionState.Paused);
        pausedAction.ShouldNotBeNull();
        actionGroup.Reasons.Count.ShouldBe(2);
    }

    [TestMethod]
    public void PauseActionGroup_UserReasonAndMaintenanceReason_StartedActionPaused_OneReason()
    {
        var agvGuid = Guid.NewGuid();
        var createdDate = DateTime.UtcNow;

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Started,
            assignedAgv: agvGuid,
            createdDate: createdDate);
        actionGroup.PauseActionGroup(PauseReason.UserMode.ToReason());

        actionGroup.PauseActionGroup(PauseReason.MaintenanceMode.ToReason());

        actionGroup.AssignedAgv.ShouldBe(agvGuid);
        actionGroup.StartedDate.ShouldNotBeNull();
        actionGroup.CompletedDate.ShouldBeNull();
        actionGroup.CreatedDate.ShouldBe(createdDate);
        actionGroup.State.ShouldBe(ActionGroupState.Paused);
        var pausedAction = actionGroup.Actions.FirstOrDefault(a => a.State == ActionState.Paused);
        pausedAction.ShouldNotBeNull();
        actionGroup.Reasons.Count.ShouldBe(1);
    }

    [TestMethod]
    public void PauseActionGroup_AgvLockReasonAndMaintenanceReason_StartedActionPaused_OneReason()
    {
        var agvGuid = Guid.NewGuid();
        var createdDate = DateTime.UtcNow;

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Started,
            assignedAgv: agvGuid,
            createdDate: createdDate);
        actionGroup.PauseActionGroup(PauseReason.LockMode.ToReason());

        actionGroup.PauseActionGroup(PauseReason.MaintenanceMode.ToReason());

        actionGroup.AssignedAgv.ShouldBe(agvGuid);
        actionGroup.StartedDate.ShouldNotBeNull();
        actionGroup.CompletedDate.ShouldBeNull();
        actionGroup.CreatedDate.ShouldBe(createdDate);
        actionGroup.State.ShouldBe(ActionGroupState.Paused);
        var pausedAction = actionGroup.Actions.FirstOrDefault(a => a.State == ActionState.Paused);
        pausedAction.ShouldNotBeNull();
        actionGroup.Reasons.Count.ShouldBe(1);
    }

    [DataRow(null, true, false)] // no previous action group, pick: true, drop: false
    [DataRow(true, true, false)] // true on last action of previous action group, pick: true, drop: false
    [DataRow(false, false, true)] // false on last action of previous action group, pick: false, drop: true
    [DataTestMethod]
    public void SetDockFrontOfAction_PickDropActionGroup_NoRotationNeeded_DockFrontSet(bool? previous, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv.ShouldBeNull();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, true, true)] // no previous action group, pick: true, drop: true
    [DataRow(true, true, true)] // true on last action of previous action group, pick: true, drop: true
    [DataRow(false, false, false)] // false on last action of previous action group, pick: false, drop: false
    [DataTestMethod]
    public void SetDockFrontOfAction_PickDropActionGroup_RotationNeeded_DockFrontSet(bool? previous, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.CargoRotation, 1d.ToString(CultureInfo.InvariantCulture) } });
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv!.NeedsRotation().ShouldBeTrue();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, true, false)] // no previous action group, pick, move: true, drop: false
    [DataRow(true, true, false)] // true on last action of previous action group, pick, move: true, drop: false
    [DataRow(false, false, true)] // false on last action of previous action group, pick, move: false, drop: true
    [DataTestMethod]
    public void SetDockFrontOfAction_IncompleteDropActionGroup_NoRotationNeeded_DockFrontSet(bool? previous, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget();
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv.ShouldBeNull();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        moveAction.InfoToAgv.ShouldBeNull();

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, true, true)] // no previous action group, pick, move: true, drop: true
    [DataRow(true, true, true)] // true on last action of previous action group, pick, move: true, drop: true
    [DataRow(false, false, false)] // false on last action of previous action group, pick, move: false, drop: false
    [DataTestMethod]
    public void SetDockFrontOfAction_IncompleteDropActionGroup_RotationNeeded_DockFrontSet(bool? previous, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.CargoRotation, 1d.ToString(CultureInfo.InvariantCulture) } });
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv!.NeedsRotation().ShouldBeTrue();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        moveAction.InfoToAgv.ShouldBeNull();

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, true, false)] // no previous action group, pick, move: true, drop: false
    [DataRow(true, true, false)] // true on last action of previous action group, pick, move: true, drop: false
    [DataRow(false, false, true)] // false on last action of previous action group, pick, move: false, drop: true
    [DataTestMethod]
    public void SetDockFrontOfAction_IncompletePickActionGroup_NoRotationNeeded_DockFrontSet(bool? previous, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget();
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv.ShouldBeNull();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        moveAction.InfoToAgv.ShouldBeNull();

        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, true, true)] // no previous action group, pick, move: true, drop: true
    [DataRow(true, true, true)] // true on last action of previous action group, pick, move: true, drop: true
    [DataRow(false, false, false)] // false on last action of previous action group, pick, move: false, drop: false
    [DataTestMethod]
    public void SetDockFrontOfAction_IncompletePickActionGroup_RotationNeeded_DockFrontSet(bool? previous, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget(infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.CargoRotation, 1d.ToString(CultureInfo.InvariantCulture) } });
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv!.NeedsRotation().ShouldBeTrue();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        moveAction.InfoToAgv.ShouldBeNull();

        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void SetDockFrontOfAction_ChargeActionGroup_DockFrontSet(bool? previous)
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        var chargeAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Charge);

        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBeTrue();

        actionGroup.SetDockFrontOfAction(chargeAction.CorrelationId, previous);
        chargeAction.InfoToAgv.ShouldNotBeNull();
        chargeAction.InfoToAgv.GetDockFront().ShouldBeTrue();
    }

    [DataRow(null, true)]
    [DataRow(true, true)]
    [DataRow(false, false)]
    [DataTestMethod]
    public void SetDockFrontOfAction_ParkActionGroup_DockFrontSet(bool? previous, bool park)
    {
        var parkAction = AgvActionGenerator.GenerateParkAction();
        var actionGroup = new ActionGroup(parkAction.ParentId, "") { Actions = [parkAction] };

        actionGroup.SetDockFrontOfAction(parkAction.CorrelationId, previous);
        parkAction.InfoToAgv.ShouldNotBeNull();
        parkAction.InfoToAgv.GetDockFront().ShouldBe(park);
    }

    [TestMethod]
    public void SetTargetForActionWithUpdate_NoDropTarget_ActionUpdated()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(pickActionState: ActionState.Completed, dropActionState: ActionState.Created, moveActionState: ActionState.Started);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        var moveAction = actionGroup.SetTargetForActionWithUpdate(dropAction.CorrelationId, "target");
        moveAction.ActionType.ShouldBe(ActionType.Drop);
        moveAction.Target.NodeId.ShouldBe("target");

        dropAction.ShouldSkip.ShouldBeTrue();
    }

    [TestMethod]
    public void SetTargetForActionWithUpdate_NoPickTarget_ActionUpdated()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget(pickActionState: ActionState.Created, dropActionState: ActionState.Created, moveActionState: ActionState.Started);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);

        var moveAction = actionGroup.SetTargetForActionWithUpdate(pickAction.CorrelationId, "target");
        moveAction.ActionType.ShouldBe(ActionType.Pick);
        moveAction.Target.NodeId.ShouldBe("target");

        pickAction.ShouldSkip.ShouldBeTrue();
    }

    [TestMethod]
    public void SetTargetForActionWithUpdate_InvalidAction_ThrowsActionNotFoundException()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(ActionState.Completed, ActionState.Started);

        Should.Throw<ActionNotFoundException>(() => actionGroup.SetTargetForActionWithUpdate(Guid.Empty, "target"));
    }

    [TestMethod]
    public void GetNextNotSkippedAction_NoSkippedAction_NextAction()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var result = actionGroup.GetNextNotSkippedAction(actionGroup.Actions[0].CorrelationId);
        result.ShouldBe(actionGroup.Actions[1]);
    }

    [TestMethod]
    public void GetNextNotSkippedAction_NoNextAction_Null()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var result = actionGroup.GetNextNotSkippedAction(actionGroup.Actions[1].CorrelationId);
        result.ShouldBeNull();
    }

    [TestMethod]
    public void GetNextNotSkippedAction_IncorrectActionId_ThrowsAgvNotFoundException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        Should.Throw<ActionNotFoundException>(() => actionGroup.GetNextNotSkippedAction(Guid.Empty));
    }

    [TestMethod]
    public void GetNextNotSkippedAction_MultipleSkippedActions_NextAction()
    {
        var agId = "actionGroup".ToGuid();
        List<TransportOrder.Domain.Models.ActionGroup.AgvAction> actions =
        [
            AgvActionGenerator.GenerateAction(agId, "node"),
            AgvActionGenerator.GenerateAction(agId, "node", ActionType.Move, 1),
            AgvActionGenerator.GenerateAction(agId, "node", ActionType.Move, 2),
            AgvActionGenerator.GenerateAction(agId, "node", ActionType.Move, 3)
        ];

        actions[1].Skip();
        actions[2].Skip();
        var actionGroup = AgvActionGenerator.GenerateActionGroup(actions);

        var result = actionGroup.GetNextNotSkippedAction(actions[0].CorrelationId);
        result.ShouldBe(actions[3]);
    }

    [TestMethod]
    public void IsOutdated_ActionGroupStartedPriorToExpirationDate_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        var expirationTime = TimeSpan.FromMinutes(2);
        var expirationDate = now.Add(-expirationTime);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState:ActionState.Started, startedDate:expirationDate.AddSeconds(-1));

        actionGroup.IsOutdated(expirationTime, now).ShouldBeTrue();
    }

    [TestMethod]
    public void IsOutdated_ActionGroupStartedAfterExpirationDate_ReturnsFalse()
    {
        var now = DateTime.UtcNow;
        var expirationTime = TimeSpan.FromMinutes(2);
        var expirationDate = now.Add(-expirationTime);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Started, startedDate: expirationDate.AddSeconds(1));

        actionGroup.IsOutdated(expirationTime, now).ShouldBeFalse();
    }

    [TestMethod]
    public void IsOutdated_PendingActionGroupCreatedPriorToExpirationDate_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        var expirationTime = TimeSpan.FromMinutes(2);
        var expirationDate = now.Add(-expirationTime);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(createdDate: expirationDate.AddSeconds(-1));

        actionGroup.IsOutdated(expirationTime, now).ShouldBeTrue();
    }

    [TestMethod]
    public void IsOutdated_PendingActionGroupAfterExpirationDate_ReturnsFalse()
    {
        var now = DateTime.UtcNow;
        var expirationTime = TimeSpan.FromMinutes(2);
        var expirationDate = now.Add(-expirationTime);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(createdDate: expirationDate.AddSeconds(1));

        actionGroup.IsOutdated(expirationTime, now).ShouldBeFalse();
    }
}