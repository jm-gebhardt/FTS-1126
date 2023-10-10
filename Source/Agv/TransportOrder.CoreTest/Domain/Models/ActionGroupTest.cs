using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvActionGenerator = Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper.AgvActionGenerator;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Models;

[TestClass]
public class ActionGroupTests
{
    private static void SetActionState(ActionGroup actionGroup, ActionState state)
    {
        foreach (var action in actionGroup.Actions)
        {
            action.UpdateActionState(state);
        }
    }

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

        actionGroup.StartAction(actionGroup.Actions.First().CorrelationId);
        actionGroup.UpdateActionGroupState();

        actionGroup.State.ShouldBe(ActionGroupState.Started);
        actionGroup.StartedDate.ShouldNotBeNull();
        actionGroup.StartedDate.Value.ShouldBeInRange(now, DateTime.UtcNow);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenActionGroupStateIsStartedAndSameFromPreviousState_UpdateActionGroupStateToStartedAndDoesNotSetStartedDate()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var previousDate = DateTime.Today.AddDays(-10);

        actionGroup.StartAction(actionGroup.Actions.First().CorrelationId);
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
        SetActionState(actionGroup, ActionState.Completed);
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
        SetActionState(actionGroup, ActionState.Completed);

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
        ((DateTime) actionGroup.CompletedDate).ShouldBeInRange(now, DateTime.UtcNow);
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
}