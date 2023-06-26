using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Extensions;

[TestClass]
public class ActionGroupExtensionsTest
{
    private static void SetActionState(ActionGroup actionGroup, ActionState state)
    {
        foreach (var action in actionGroup.Actions)
        {
            action.State = state;
        }
    }

    [TestMethod]
    [DataRow(ActionState.Created, ActionState.Created, ActionGroupState.Created)]
    [DataRow(ActionState.Started, ActionState.Created, ActionGroupState.Started)]
    [DataRow(ActionState.Completed, ActionState.Created, ActionGroupState.Started)]
    [DataRow(ActionState.Completed, ActionState.Started, ActionGroupState.Started)]
    [DataRow(ActionState.Completed, ActionState.Completed, ActionGroupState.Completed)]
    public void VerifyAllStateTransition_ForActionGroupWithTwoActions(ActionState actionState1, ActionState actionState2, ActionGroupState actionGroupState)
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        actionGroup.Actions.ElementAt(0).State = actionState1;
        actionGroup.Actions.ElementAt(1).State = actionState2;
        actionGroup.UpdateActionGroupState();
        actionGroup.State.ShouldBe(actionGroupState);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenAllActionsInStateCreated_SetActionGroupStateToCreated()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        SetActionState(actionGroup, ActionState.Created);

        actionGroup.UpdateActionGroupState();

        actionGroup.State.ShouldBe(ActionGroupState.Created);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenActionGroupStateIsStartedAndDifferentFromPreviousState_UpdateActionGroupStateToStartedAndSetStartedDate()
    {
        var now = DateTime.UtcNow;
        TimeProvider.Set(() => now);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        SetActionState(actionGroup, ActionState.Created);
        actionGroup.State = ActionGroupState.Created;
        actionGroup.Actions.First().State = ActionState.Started;
        actionGroup.StartedDate.ShouldBeNull();

        actionGroup.UpdateActionGroupState();

        actionGroup.State.ShouldBe(ActionGroupState.Started);
        actionGroup.StartedDate.ShouldNotBeNull();
        actionGroup.StartedDate.Value.ShouldBe(now);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenActionGroupStateIsStartedAndSameFromPreviousState_UpdateActionGroupStateToStartedAndDoesNotSetStartedDate()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        const ActionGroupState actionGroupState = ActionGroupState.Started;
        var previousDate = DateTime.Today.AddDays(-10);
        SetActionState(actionGroup, ActionState.Created);
        actionGroup.State = actionGroupState;
        actionGroup.Actions.First().State = ActionState.Started;
        actionGroup.StartedDate = previousDate;

        actionGroup.UpdateActionGroupState();

        actionGroup.State.ShouldBe(actionGroupState);
        actionGroup.StartedDate.Value.ShouldBe(previousDate);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenActionGroupStateIsCompletedAndDifferentFromPreviousState_UpdateActionGroupStateToCompletedAndSetCompletedDate()
    {
        var now = DateTime.UtcNow;
        TimeProvider.Set(() => now);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        SetActionState(actionGroup, ActionState.Completed);
        actionGroup.State = ActionGroupState.Started;
        actionGroup.CompletedDate.ShouldBeNull();

        actionGroup.UpdateActionGroupState();

        actionGroup.State.ShouldBe(ActionGroupState.Completed);
        actionGroup.CompletedDate.ShouldNotBeNull();
        actionGroup.CompletedDate.Value.ShouldBe(now);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenActionGroupStateIsCompletedAndSameFromPreviousState_UpdateActionGroupStateToCompletedAndDoesNotSetCompletedDate()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        const ActionGroupState actionGroupState = ActionGroupState.Completed;
        var previousDate = DateTime.Today.AddDays(-10);
        SetActionState(actionGroup, ActionState.Completed);
        actionGroup.State = actionGroupState;
        actionGroup.CompletedDate = previousDate;

        actionGroup.UpdateActionGroupState();

        actionGroup.State.ShouldBe(actionGroupState);
        actionGroup.CompletedDate.ShouldNotBeNull();
        actionGroup.CompletedDate.Value.Date.ShouldBe(previousDate);
    }

    [TestMethod]
    public void AssignAgvToActionGroup_WhenActionGroupAssignedAgvIsNull_SetActionGroupAssignedAgv()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agvToAssign = AgvDummyGenerator.GenerateAgv();
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
        var agvToAssign = AgvDummyGenerator.GenerateAgv();
        actionGroup.AssignedAgv = actionGroupAssignedAgv;

        Should.Throw<ActionGroupAlreadyAssignedException>(() => actionGroup.Assign(agvToAssign));
    }

    [TestMethod]
    public void GetRequiredActions_WhenThereAreActionsInGroup_ReturnActions()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        ICollection<AgvAction> expectedActions = actionGroup.Actions;

        ICollection<AgvAction> actions = actionGroup.GetRequiredActions();

        actions.ShouldBe(expectedActions);
    }

    [TestMethod]
    public void GetRequiredActions_WhenThereAreNoActionsInGroup_ThrowActionsInActionGroupEmptyException()
    {
        var actionGroup = AgvActionGenerator.GenerateActionGroup(new List<AgvAction>());

        Should.Throw<ActionsInActionGroupEmptyException>(() => actionGroup.GetRequiredActions());
    }
}