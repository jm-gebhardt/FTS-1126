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
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Routing.Models;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;
using DateTime = System.DateTime;

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
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed,
            assignedAgv: Guid.NewGuid(),
            startedDate: DateTime.UtcNow,
            completedDate: DateTime.UtcNow);
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
    public void ResetActionGroup_ManualActionGroup_DoesNotResetAssignedAgv()
    {
        var createdDate = DateTime.UtcNow;
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Started,
            assignedAgv: agv,
            createdDate: createdDate);
        actionGroup.ResetActionGroup(false);

        actionGroup.AssignedAgv.ShouldNotBeNull();
        actionGroup.AssignedAgv.ShouldBe(agv);
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
    public async Task PauseAndResumeActionGroup_PausedOnce_DurationSetCorrectly()
    {
        var waitingTime = TimeSpan.FromSeconds(2);
        var agvGuid = Guid.NewGuid();
        var createdDate = DateTime.UtcNow;

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            assignedAgv: agvGuid,
            createdDate: createdDate);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        // Run pick action
        actionGroup.StartAction(pickAction.CorrelationId, [], []);
        await Task.Delay(waitingTime);
        actionGroup.CompleteAction(pickAction.CorrelationId);

        // Start drop action
        actionGroup.StartAction(dropAction.CorrelationId, [], []);
        await Task.Delay(waitingTime);

        // Pause
        actionGroup.PauseActionGroup(PauseReason.MaintenanceMode.ToReason());
        await Task.Delay(waitingTime);

        // Resume
        actionGroup.ResumeActionGroup(false);
        actionGroup.StartAction(dropAction.CorrelationId, [], []);
        await Task.Delay(waitingTime);

        // Complete
        actionGroup.CompleteAction(dropAction.CorrelationId);

        double actionDurations = actionGroup.Actions.Select(a => a.Duration!.Value.TotalSeconds).Sum();
        actionGroup.Duration!.Value.TotalSeconds.ShouldBe(actionDurations, tolerance: 1.5); // tolerance is N*truncation of double to long (in each ResumeActionGroup)
    }

    [TestMethod]
    public async Task PauseAndResumeActionGroup_MoveAction_PausedOnce_DurationSetCorrectly()
    {
        var waitingTime = TimeSpan.FromSeconds(2);
        var agvGuid = Guid.NewGuid();

        var actionGroup = AgvActionGenerator.GenerateParkActionGroup(assignedAgv: agvGuid);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);

        // Start move action
        actionGroup.StartAction(moveAction.CorrelationId, [], []);
        await Task.Delay(waitingTime);

        // Pause
        actionGroup.PauseActionGroup(PauseReason.MaintenanceMode.ToReason());
        await Task.Delay(waitingTime);

        // Resume
        actionGroup.ResumeActionGroup(false);
        actionGroup.StartAction(moveAction.CorrelationId, [], []);
        await Task.Delay(waitingTime);

        // Complete
        actionGroup.CompleteAction(moveAction.CorrelationId);

        double actionDurations = actionGroup.Actions.Select(a => a.Duration!.Value.TotalSeconds).Sum();
        actionGroup.Duration!.Value.TotalSeconds.ShouldBe(actionDurations, tolerance: 1.5);
    }

    [TestMethod]
    public async Task PauseAndResumeActionGroup_MoveAction_PausedMultipleTimes_DurationSetCorrectly()
    {
        var waitingTime = TimeSpan.FromSeconds(1);
        var agvGuid = Guid.NewGuid();

        var actionGroup = AgvActionGenerator.GenerateParkActionGroup(assignedAgv: agvGuid);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);

        // Start move action
        actionGroup.StartAction(moveAction.CorrelationId, [], []);
        await Task.Delay(waitingTime);

        // Pause
        actionGroup.PauseActionGroup(PauseReason.MaintenanceMode.ToReason());
        await Task.Delay(waitingTime);

        // Resume
        actionGroup.ResumeActionGroup(false);
        actionGroup.StartAction(moveAction.CorrelationId, [], []);
        await Task.Delay(waitingTime);

        // Pause again
        actionGroup.PauseActionGroup(PauseReason.MaintenanceMode.ToReason());
        await Task.Delay(waitingTime);

        // Resume again
        actionGroup.ResumeActionGroup(false);
        actionGroup.StartAction(moveAction.CorrelationId, [], []);
        await Task.Delay(waitingTime);

        // Complete
        actionGroup.CompleteAction(moveAction.CorrelationId);

        actionGroup.Duration!.Value.TotalSeconds.ShouldBe(moveAction.Duration!.Value.TotalSeconds, tolerance: 2.5); // tolerance is N*truncation of double to long (in each ResumeActionGroup)
    }

    [TestMethod]
    public async Task PauseAndResumeActionGroup_MoveAction_Paused2days_DurationSetCorrectly()
    {
        var waitingTime = TimeSpan.FromSeconds(2);
        var agvGuid = Guid.NewGuid();

        var actionGroup = AgvActionGenerator.GenerateParkActionGroup(assignedAgv: agvGuid);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);

        // Start move action
        actionGroup.StartAction(moveAction.CorrelationId, [], []);
        await Task.Delay(waitingTime);

        // Pause
        actionGroup.PauseActionGroup(PauseReason.MaintenanceMode.ToReason());
        // Set dates to 2 days ago
        actionGroup.GetType().GetProperty(nameof(actionGroup.StartedDate))!.SetValue(actionGroup, actionGroup.StartedDate.GetValueOrDefault().AddDays(-2));
        moveAction.GetType().GetProperty(nameof(moveAction.StartedDate))!.SetValue(moveAction, moveAction.StartedDate.GetValueOrDefault().AddDays(-2));
        moveAction.GetType().GetProperty(nameof(moveAction.PausedDate))!.SetValue(moveAction, moveAction.PausedDate.GetValueOrDefault().AddDays(-2));

        // Resume
        actionGroup.ResumeActionGroup(false);
        actionGroup.StartAction(moveAction.CorrelationId, [], []);
        await Task.Delay(waitingTime);

        // Complete
        actionGroup.CompleteAction(moveAction.CorrelationId);

        double actionDurations = actionGroup.Actions.Select(a => a.Duration!.Value.TotalSeconds).Sum();
        actionGroup.Duration!.Value.TotalSeconds.ShouldBe(actionDurations, tolerance: 1.5); // tolerance is N*truncation of double to long (in each ResumeActionGroup)
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

    [DataRow(null, DockingDirection.Both, true, false)] // target node allows both directions, no previous action group, pick: true, drop: false
    [DataRow(true, DockingDirection.Both, false, true)] // target node allows both directions, true on last action of previous action group, pick: false, drop: true
    [DataRow(false, DockingDirection.Both, true, false)] // target node allows both directions, false on last action of previous action group, pick: true, drop: false
    [DataRow(false, DockingDirection.Front, true, true)] // target node docking is front, false on last action of previous action group, pick: true, drop: true
    [DataRow(false, DockingDirection.Back, false, false)] // target node docking is back, false on last action of previous action group, pick: false, drop: false
    [DataTestMethod]
    public void SetDockFrontOfAction_PickDropActionGroup_NoRotationNeeded_DockFrontSet(bool? previous, DockingDirection? nodeDockingDirection, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv.ShouldBeNull();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous, nodeDockingDirection, true);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous, nodeDockingDirection, true);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(true, true, false)] // true on last action of previous action group, pick: true, drop: true
    [DataRow(false, false, true)] //  false on last action of previous action group, pick: false, drop: true
    [DataTestMethod]
    public void SetDockFrontOfAction_PickDropActionGroup_Doppelspiel_DockFrontSet(bool? previous, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv.ShouldBeNull();
        pickAction.Nodes = ["target of last action"];
        pickAction.Nodes.Length.ShouldBe(1);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous, DockingDirection.Both);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous, DockingDirection.Both);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, DockingDirection.Back, true)]
    [DataRow(true, DockingDirection.Back, true)]
    [DataRow(false, DockingDirection.Back, false)]
    [DataTestMethod]
    public void SetDockFrontOfAction_ManualPickDropActionGroup_ExternalValue_DockFrontSetToExternalValueNodeDockingDirectionIgnored(bool? previous, DockingDirection? nodeDockingDirection, bool externalValue)
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        actionGroup.IsManual = true;
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.SetDockFrontValue(externalValue);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.SetDockFrontValue(externalValue);

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous, nodeDockingDirection);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(externalValue);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous, nodeDockingDirection);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(externalValue);
    }

    [DataRow(null, true, false)] // no previous action group, pick: true, drop: false
    [DataRow(true, false, true)] // true on last action of previous action group, pick: false, drop: true
    [DataRow(false, true, false)] // false on last action of previous action group, pick: true, drop: false
    [DataTestMethod]
    public void SetDockFrontOfAction_AutomaticPickDropActionGroup_ExternalValue_DockFrontSet_ExternalValueIgnored(bool? previous, bool pick, bool drop)
    {
        const bool external = true;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.SetDockFrontValue(external);
        pickAction.InfoToAgv.ShouldNotBeNull();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.SetDockFrontValue(external);
        dropAction.InfoToAgv.ShouldNotBeNull();

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous, DockingDirection.Both, true);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous, DockingDirection.Both, true);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, DockingDirection.Both, true, false)] // target node allows both directions, no previous action group, pick: true, drop: false
    [DataRow(true, DockingDirection.Both, false, false)] // target node allows both directions, true on last action of previous action group, pick: false, drop: false
    [DataRow(false, DockingDirection.Both, true, false)] // target node allows both directions, false on last action of previous action group, pick: true, drop: false
    [DataRow(false, DockingDirection.Front, true, true)] // target node docking is front, false on last action of previous action group, pick: true, drop: true
    [DataRow(false, DockingDirection.Back, false, false)] // target node docking is back, false on last action of previous action group, pick: false, drop: false
    [DataTestMethod]
    public void SetDockFrontOfAction_PickDropActionGroup_NoRotationNeeded_ForceDropBackWards_DockFrontSet(bool? previous, DockingDirection? nodeDockingDirection, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(dropInfoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.ForceDroppingBackwards, true.ToString(CultureInfo.InvariantCulture) } });
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous, nodeDockingDirection, true);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous, nodeDockingDirection, true);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, DockingDirection.Both, true, true)] // target node allows both directions, no previous action group, pick: true, drop: true
    [DataRow(true, DockingDirection.Both, false, false)] // target node allows both directions, true on last action of previous action group, pick: false, drop: false
    [DataRow(false, DockingDirection.Both, true, true)] // target node allows both directions, false on last action of previous action group, pick: true, drop: true
    [DataRow(false, DockingDirection.Front, true, true)] // target node docking is front, false on last action of previous action group, pick: true, drop: true
    [DataRow(false, DockingDirection.Back, false, false)] // target node docking is back, false on last action of previous action group, pick: false, drop: false
    [DataTestMethod]
    public void SetDockFrontOfAction_PickDropActionGroup_RotationNeeded_DockFrontSet(bool? previous, DockingDirection? nodeDockingDirection, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.CargoRotation, 1d.ToString(CultureInfo.InvariantCulture) } });
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv!.NeedsRotation().ShouldBeTrue();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous, nodeDockingDirection, true);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous, nodeDockingDirection, true);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, DockingDirection.Both, true, false)] // target node allows both directions, no previous action group, pick: true, drop: false
    [DataRow(true, DockingDirection.Both, false, false)] // target node allows both directions, true on last action of previous action group, pick: false, drop: false
    [DataRow(false, DockingDirection.Both, true, false)] // target node allows both directions, false on last action of previous action group, pick: true, drop: false
    [DataRow(false, DockingDirection.Front, true, true)] // target node docking is front, false on last action of previous action group, pick: true, drop: true
    [DataRow(false, DockingDirection.Back, false, false)] // target node docking is back, false on last action of previous action group, pick: false, drop: false
    [DataTestMethod]
    public void SetDockFrontOfAction_PickDropActionGroup_RotationNeeded_ForceDropBackWards_DockFrontSet(bool? previous, DockingDirection? nodeDockingDirection, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(infoToAgv: new Dictionary<InfoToAgvKey, string>
            {
                {
                    InfoToAgvKey.CargoRotation, 1d.ToString(CultureInfo.InvariantCulture)
                }
            },
            dropInfoToAgv: new Dictionary<InfoToAgvKey, string>
            {
                {
                    InfoToAgvKey.ForceDroppingBackwards, true.ToString(CultureInfo.InvariantCulture)
                }
            });
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv!.NeedsRotation().ShouldBeTrue();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous, nodeDockingDirection, true);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous, nodeDockingDirection, true);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, true, false, false)]
    [DataRow(true, false, true, true)]
    [DataRow(false, true, false, false)]
    [DataTestMethod]
    public void SetDockFrontOfAction_IncompleteDropActionGroup_NoRotationNeeded_DockFrontSet(bool? previous, bool pick, bool move, bool drop)
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget();
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv.ShouldBeNull();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        moveAction.InfoToAgv.ShouldBeNull();

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous, DockingDirection.Both, true);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous, DockingDirection.Both);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBe(move);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous, DockingDirection.Both, true);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, true, false, true)]
    [DataRow(true, false, true, false)]
    [DataRow(false, true, false, true)]
    [DataTestMethod]
    public void SetDockFrontOfAction_IncompleteDropActionGroup_RotationNeeded_DockFrontSet(bool? previous, bool move, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.CargoRotation, 1d.ToString(CultureInfo.InvariantCulture) } });
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv!.NeedsRotation().ShouldBeTrue();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        moveAction.InfoToAgv.ShouldBeNull();

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous, DockingDirection.Both, true);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(move);

        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous, DockingDirection.Both, true);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous, DockingDirection.Both);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, true, true, false)] // no previous action group, move: true, pick: true, drop: false
    [DataRow(true, false, false, true)] // true on last action of previous action group, move false:, pick: false, drop: false
    [DataRow(false, true, true, false)] // false on last action of previous action group, pick, move: true, drop: false
    [DataTestMethod]
    public void SetDockFrontOfAction_IncompletePickActionGroup_NoDoppelspiel_NoRotationNeeded_DockFrontSet(bool? previous, bool move, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget();
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv.ShouldBeNull();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        moveAction.InfoToAgv.ShouldBeNull();

        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous, DockingDirection.Both, true);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBe(move);

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous, DockingDirection.Both);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous, DockingDirection.Both, true);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, true, true, true)] // no previous action group, pick, move: true, drop: true
    [DataRow(true, false, false, false)] // true on last action of previous action group, pick, move: false, drop: false
    [DataRow(false, true, true, true)] // false on last action of previous action group, pick, move: true, drop: true
    [DataTestMethod]
    public void SetDockFrontOfAction_IncompletePickActionGroup_RotationNeeded_DockFrontSet(bool? previous, bool move, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget(infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.CargoRotation, 1d.ToString(CultureInfo.InvariantCulture) } });
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv!.NeedsRotation().ShouldBeTrue();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        moveAction.InfoToAgv.ShouldBeNull();

        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous, DockingDirection.Both, true);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBe(move);

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous, DockingDirection.Both);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous, DockingDirection.Both, true);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, true, true, true)] // no previous action group, move: true, pick: true, drop: true
    [DataRow(true, false, false, false)] // true on last action of previous action group, move false:, pick: false, drop: false
    [DataRow(false, true, true, true)] // false on last action of previous action group, pick, move: true, drop: true
    [DataTestMethod]
    public void SetDockFrontOfAction_IncompletePickActionGroup_UpdateOfMoveAction_NeedsRotation_DockFrontSet(bool? previous, bool move, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget(infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.CargoRotation, 1d.ToString(CultureInfo.InvariantCulture) } });
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv!.NeedsRotation().ShouldBeTrue();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        moveAction.InfoToAgv.ShouldBeNull();

        // Start move action
        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous, DockingDirection.Both, true);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBe(move);

        // Update target
        var currentAction = actionGroup.SetTargetForActionWithUpdate(pickAction.CorrelationId, "target", new Route(32, [], []));
        actionGroup.SetDockFrontOfAction(currentAction.CorrelationId, previous, DockingDirection.Both, true);
        currentAction.ShouldNotBeNull();
        currentAction.InfoToAgv.ShouldNotBeNull();
        currentAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        // Start drop action
        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous, DockingDirection.Both, true);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, true, true, false)] // no previous action group, move: true, pick: true, drop: false
    [DataRow(true, false, false, true)] // true on last action of previous action group, move false:, pick: false, drop: true
    [DataRow(false, true, true, false)] // false on last action of previous action group, pick, move: true, drop: false
    [DataTestMethod]
    public void SetDockFrontOfAction_IncompletePickActionGroup_UpdateOfMoveAction_NeedsNoRotation_DockFrontSet(bool? previous, bool move, bool pick, bool drop)
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget(infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.CargoRotation, 0d.ToString(CultureInfo.InvariantCulture) } });
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv!.NeedsRotation().ShouldBeFalse();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        moveAction.InfoToAgv.ShouldBeNull();

        // Start move action
        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous, DockingDirection.Both, true);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBe(move);

        // Update target
        var currentAction = actionGroup.SetTargetForActionWithUpdate(pickAction.CorrelationId, "target", new Route(32, [], []));
        actionGroup.SetDockFrontOfAction(currentAction.CorrelationId, previous, DockingDirection.Both, true);
        currentAction.ShouldNotBeNull();
        currentAction.InfoToAgv.ShouldNotBeNull();
        currentAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        // Start drop action
        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, previous, DockingDirection.Both, true);
        dropAction.InfoToAgv.ShouldNotBeNull();
        dropAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, true, false, true)]
    [DataRow(true, false, true, false)]
    [DataRow(false, true, false, true)]
    [DataTestMethod]
    public void SetDockFrontOfAction_IncompleteDropActionGroup_UpdateOfMoveAction_NeedsRotation_DockFrontSet(bool? previous, bool pick, bool move, bool drop)
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.CargoRotation, 1d.ToString(CultureInfo.InvariantCulture) } });
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv!.NeedsRotation().ShouldBeTrue();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        moveAction.InfoToAgv.ShouldBeNull();

        // Start pick action
        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous, DockingDirection.Both, true);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        // Start move action
        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous, DockingDirection.Both, true);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBe(move);

        // Update target
        var currentAction = actionGroup.SetTargetForActionWithUpdate(dropAction.CorrelationId, "target", new Route(32, [], []));
        actionGroup.SetDockFrontOfAction(currentAction.CorrelationId, previous, DockingDirection.Both);
        currentAction.ShouldNotBeNull();
        currentAction.InfoToAgv.ShouldNotBeNull();
        currentAction.InfoToAgv.GetDockFront().ShouldBe(drop);
    }

    [DataRow(null, true, false, false)]
    [DataRow(true, false, true, true)]
    [DataRow(false, true, false, false)]
    [DataTestMethod]
    public void SetDockFrontOfAction_IncompleteDropActionGroup_UpdateOfMoveAction_NoRotationNeeded_DockFrontSet(bool? previous, bool pick, bool move, bool drop)
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget(infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.CargoRotation, 0d.ToString(CultureInfo.InvariantCulture) } });
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        pickAction.InfoToAgv!.NeedsRotation().ShouldBeFalse();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        dropAction.InfoToAgv.ShouldBeNull();
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        moveAction.InfoToAgv.ShouldBeNull();

        // Start pick action
        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, previous, DockingDirection.Both, true);
        pickAction.InfoToAgv.ShouldNotBeNull();
        pickAction.InfoToAgv.GetDockFront().ShouldBe(pick);

        // Start move action
        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous, DockingDirection.Both, true);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBe(move);

        // Update target
        var currentAction = actionGroup.SetTargetForActionWithUpdate(dropAction.CorrelationId, "target", new Route(32, [], []));
        actionGroup.SetDockFrontOfAction(currentAction.CorrelationId, previous, DockingDirection.Both);
        currentAction.ShouldNotBeNull();
        currentAction.InfoToAgv.ShouldNotBeNull();
        currentAction.InfoToAgv.GetDockFront().ShouldBe(drop);
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

        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous, DockingDirection.Front);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBeTrue();

        actionGroup.SetDockFrontOfAction(chargeAction.CorrelationId, previous, DockingDirection.Front);
        chargeAction.InfoToAgv.ShouldNotBeNull();
        chargeAction.InfoToAgv.GetDockFront().ShouldBeTrue();
    }

    [DataRow(null, true)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataTestMethod]
    public void SetDockFrontOfAction_ParkActionGroup_DockFrontSet(bool? previous, bool park)
    {
        var parkAction = AgvActionGenerator.GenerateParkAction();
        var actionGroup = new ActionGroup(parkAction.ParentId, "") { Actions = [parkAction] };

        actionGroup.SetDockFrontOfAction(parkAction.CorrelationId, previous, DockingDirection.Both);
        parkAction.InfoToAgv.ShouldNotBeNull();
        parkAction.InfoToAgv.GetDockFront().ShouldBe(park);
    }

    [DataRow(false, null, true)]
    [DataRow(false, true, true)]
    [DataRow(false, false, false)]
    [DataRow(true, false, true)]
    [DataTestMethod]
    public void SetDockFrontOfAction_QueueActionGroup_DockFrontSet(bool startIsStation, bool? previous, bool move)
    {
        var queueAction = AgvActionGenerator.GenerateParkAction();
        var actionGroup = new ActionGroup(queueAction.ParentId, "") { Actions = [queueAction], HasPriority = true };

        actionGroup.SetDockFrontOfAction(queueAction.CorrelationId, previous, DockingDirection.Both, startIsStation);
        queueAction.InfoToAgv.ShouldNotBeNull();
        queueAction.InfoToAgv.GetDockFront().ShouldBe(move);
    }

    [DataRow(false, null, true)]
    [DataRow(false, true, true)]
    [DataRow(false, false, false)]
    [DataRow(true, false, true)]
    [DataTestMethod]
    public void SetDockFrontOfAction_ManualMove_DockFrontSet(bool startIsStation, bool? previous, bool move)
    {
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup(isManual: true);
        var moveAction = actionGroup.Actions.Single();
        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, previous, DockingDirection.Both, startIsStation);
        moveAction.InfoToAgv.ShouldNotBeNull();
        moveAction.InfoToAgv.GetDockFront().ShouldBe(move);
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
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Started, startedDate: expirationDate.AddSeconds(-1));

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

    [TestMethod]
    public void SetCargoLiftHeightForAction_PickAction_PickLiftHeightSet()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);

        const double liftHeight = 1457.331;

        actionGroup.SetCargoLiftHeightForAction(pickAction.CorrelationId, liftHeight, 42);

        pickAction.InfoToAgv!.GetLiftHeight().ShouldBe(liftHeight);
    }

    [TestMethod]
    public void SetCargoLiftHeightForAction_DropAction_DropLiftHeightSet()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);

        const double liftHeight = 1457.331;

        actionGroup.SetCargoLiftHeightForAction(dropAction.CorrelationId, 42, liftHeight);

        dropAction.InfoToAgv!.GetLiftHeight().ShouldBe(liftHeight);
    }

    [DataRow(1457.331, null, 1457.331)]
    [DataRow(null, 1457.331, 1457.331)]
    [DataRow(null, null, null)]
    [DataTestMethod]
    public void SetCargoLiftHeightForAction_MoveAction_LiftHeightSet(double? pickLiftHeight, double? dropLiftHeight, double? expected)
    {
        var actionGroup = AgvActionGenerator.GenerateParkActionGroup();
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);

        actionGroup.SetCargoLiftHeightForAction(moveAction.CorrelationId, pickLiftHeight, dropLiftHeight);

        moveAction.InfoToAgv?.GetLiftHeight().ShouldBe(expected);
    }

    [DataRow(ActionState.Completed, ActionState.Created, true)]
    [DataRow(ActionState.Completed, ActionState.Started, true)]
    [DataRow(ActionState.Completed, ActionState.Paused, true)]
    [DataRow(ActionState.Completed, ActionState.Aborted, true)]
    [DataRow(ActionState.Completed, ActionState.Completed, false)]
    [DataRow(ActionState.Created, ActionState.Created, false)]
    [DataRow(ActionState.Started, ActionState.Created, false)]
    [DataRow(ActionState.Paused, ActionState.Created, false)]
    [DataRow(ActionState.Aborted, ActionState.Created, false)]
    [DataRow(ActionState.Started, ActionState.Completed, false)] // Drop  manually completed via FE 
    [DataTestMethod]
    public void CheckLoadStateMatches_HasCargo_ReturnsExpectedValue(ActionState pickActionState, ActionState dropActionState, bool expected)
    {
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: pickActionState, dropActionState: dropActionState, assignedAgv: agv);

        actionGroup.CheckLoadStateMatches(true, agv).ShouldBe(expected);
    }

    [DataRow(ActionState.Completed, ActionState.Completed, true)]
    [DataRow(ActionState.Created, ActionState.Created, true)]
    [DataRow(ActionState.Completed, ActionState.Aborted, false)]
    [DataRow(ActionState.Completed, ActionState.Started, false)]
    [DataRow(ActionState.Completed, ActionState.Paused, false)]
    [DataRow(ActionState.Started, ActionState.Created, true)]
    [DataRow(ActionState.Paused, ActionState.Created, true)]
    [DataRow(ActionState.Aborted, ActionState.Created, true)]
    [DataRow(ActionState.Started, ActionState.Completed, true)] // Drop  manually completed via FE 
    [DataTestMethod]
    public void CheckLoadStateMatches_HasNoCargo_ReturnsExpectedValue(ActionState pickActionState, ActionState dropActionState, bool expected)
    {
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: pickActionState, dropActionState: dropActionState, assignedAgv: agv);

        actionGroup.CheckLoadStateMatches(false, agv).ShouldBe(expected);
    }

    [DataRow(true, ActionType.Pick, ActionState.Completed, true)]
    [DataRow(false, ActionType.Pick, ActionState.Completed, false)]
    [DataRow(true, ActionType.Pick, ActionState.Started, false)]
    [DataRow(false, ActionType.Pick, ActionState.Started, true)]
    [DataRow(true, ActionType.Pick, ActionState.Created, false)]
    [DataRow(false, ActionType.Pick, ActionState.Created, true)]
    [DataRow(true, ActionType.Pick, ActionState.Paused, false)]
    [DataRow(false, ActionType.Pick, ActionState.Paused, true)]
    [DataRow(true, ActionType.Drop, ActionState.Completed, false)]
    [DataRow(false, ActionType.Drop, ActionState.Completed, true)]
    [DataRow(true, ActionType.Drop, ActionState.Started, true)]
    [DataRow(false, ActionType.Drop, ActionState.Started, false)]
    [DataRow(true, ActionType.Drop, ActionState.Created, true)]
    [DataRow(false, ActionType.Drop, ActionState.Created, false)]
    [DataRow(true, ActionType.Drop, ActionState.Paused, true)]
    [DataRow(false, ActionType.Drop, ActionState.Paused, false)]
    [DataTestMethod]
    public void CheckLoadStateMatches_OnlyPickOrDrop_ReturnsExpectedValue(bool hasCargo, ActionType actionType, ActionState actionState, bool expected)
    {
        var agv = Guid.NewGuid();
        var action = new TransportOrder.Domain.Models.ActionGroup.AgvAction() { CorrelationId = Guid.NewGuid(), ParentId = Guid.NewGuid(), State = actionState, ActionType = actionType };
        var actionGroup = new ActionGroup() { CorrelationId = action.ParentId, Actions = [action], AssignedAgv = agv };

        actionGroup.CheckLoadStateMatches(hasCargo, agv).ShouldBe(expected);
    }

    [TestMethod]
    public void CheckLoadStateMatches_AgvNotAssignedToActionGroup_ThrowsAgvNotAssignedToActionGroupException()
    {
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Created, assignedAgv: agv);

        Should.Throw<AgvNotAssignedToActionGroupException>(() => actionGroup.CheckLoadStateMatches(true, Guid.NewGuid()));
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void CheckLoadStateMatches_ChargeActionGroup_ReturnsExpectedValue(bool hasCargo)
    {
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv);

        actionGroup.CheckLoadStateMatches(hasCargo, agv).ShouldBe(!hasCargo);
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void CheckLoadStateMatches_MoveActionGroup_ReturnsExpectedValue(bool hasCargo)
    {
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup(assignedAgv: agv);

        actionGroup.CheckLoadStateMatches(hasCargo, agv).ShouldBe(!hasCargo);
    }

    [DataRow(ActionType.Pick, true, false)]
    [DataRow(ActionType.Drop, true, true)]
    [DataRow(ActionType.Pick, false, true)]
    [DataRow(ActionType.Drop, false, false)]
    [DataTestMethod]
    public void CheckLoadStateMatches_OnlyPickOrDrop_CreatedState_ReturnsExpectedValue(ActionType actionType, bool hasCargo, bool expected)
    {
        var agv = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var testActions = new List<TransportOrder.Domain.Models.ActionGroup.AgvAction>
        {
            new(Guid.NewGuid(), actionType, "node", orderId)
            {
                Sequence = 0
            }
        };

        var actionGroup = new ActionGroup(
            orderId,
            orderId.ToString(),
            testActions,
            ActionGroupCreator.TransportOrderWebApi,
            true,
            DateTime.UtcNow,
            agv);

        actionGroup.CheckLoadStateMatches(hasCargo, agv).ShouldBe(expected);
    }

    [DataRow(ActionType.Pick, true, false)]
    [DataRow(ActionType.Drop, true, true)]
    [DataRow(ActionType.Pick, false, true)]
    [DataRow(ActionType.Drop, false, false)]
    [DataTestMethod]
    public void CheckLoadStateMatches_OnlyPickOrDrop_StartedState_ReturnsExpectedValue(ActionType actionType, bool hasCargo, bool expected)
    {
        var agv = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var testActions = new List<TransportOrder.Domain.Models.ActionGroup.AgvAction>
        {
            new(Guid.NewGuid(), actionType, "node", orderId)
            {
                Sequence = 0
            }
        };

        var actionGroup = new ActionGroup(
            orderId,
            orderId.ToString(),
            testActions,
            ActionGroupCreator.TransportOrderWebApi,
            true,
            DateTime.UtcNow,
            agv);

        actionGroup.StartAction(actionGroup.Actions.Single().CorrelationId, [], []);

        actionGroup.CheckLoadStateMatches(hasCargo, agv).ShouldBe(expected);
    }

    [DataRow(ActionType.Pick, true, true)]
    [DataRow(ActionType.Drop, true, false)]
    [DataRow(ActionType.Pick, false, false)]
    [DataRow(ActionType.Drop, false, true)]
    [DataTestMethod]
    public void CheckLoadStateMatches_OnlyPickOrDrop_CompletedState_ReturnsExpectedValue(ActionType actionType, bool hasCargo, bool expected)
    {
        var agv = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var testActions = new List<TransportOrder.Domain.Models.ActionGroup.AgvAction>
        {
            new(Guid.NewGuid(), actionType, "node", orderId)
            {
                Sequence = 0
            }
        };

        var actionGroup = new ActionGroup(
            orderId,
            orderId.ToString(),
            testActions,
            ActionGroupCreator.TransportOrderWebApi,
            true,
            DateTime.UtcNow,
            agv);

        actionGroup.StartAction(actionGroup.Actions.Single().CorrelationId, [], []);
        actionGroup.CompleteAction(actionGroup.Actions.Single().CorrelationId);

        actionGroup.CheckLoadStateMatches(hasCargo, agv).ShouldBe(expected);
    }
}