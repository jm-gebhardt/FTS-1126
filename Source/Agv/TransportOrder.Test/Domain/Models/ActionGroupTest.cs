using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Test.Extensions;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

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
        string[] expectedTargets = [actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).TargetNodeId, actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).TargetNodeId];

        actionGroup.GetTargets().ShouldBe(expectedTargets);
    }

    [TestMethod]
    public void GetTargets_IncompletePickDropActionGroupWithoutDropTarget_TargetNodeIdsOfPickAndMoveAction()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget();
        string[] expectedTargets = [actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).TargetNodeId, actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).TargetNodeId];

        actionGroup.GetTargets().ShouldBe(expectedTargets);
    }

    [TestMethod]
    public void GetTargets_IncompletePickDropActionGroupWithoutPickTarget_TargetNodeIdOfMoveAction()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget();
        string[] expectedTargets = [actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).TargetNodeId];

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
        action.TargetNodeId.ShouldBe(target);
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
}