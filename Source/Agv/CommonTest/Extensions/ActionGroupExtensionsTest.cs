using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.CommonTest.Helpers;
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
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        SetActionState(actionGroup, ActionState.Created);
        actionGroup.State = ActionGroupState.Created;
        actionGroup.Actions.First().State = ActionState.Started;
        actionGroup.StartedDate.ShouldBeNull();

        actionGroup.UpdateActionGroupState();

        actionGroup.State.ShouldBe(ActionGroupState.Started);
        actionGroup.StartedDate.ShouldNotBeNull();
        actionGroup.StartedDate.Value.Date.ShouldBe(DateTime.Today);
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
        actionGroup.StartedDate.ShouldNotBeNull();
        actionGroup.StartedDate.Value.Date.ShouldBe(previousDate);
    }

    [TestMethod]
    public void UpdateActionGroupState_WhenActionGroupStateIsCompletedAndDifferentFromPreviousState_UpdateActionGroupStateToCompletedAndSetCompletedDate()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        SetActionState(actionGroup, ActionState.Completed);
        actionGroup.State = ActionGroupState.Started;
        actionGroup.CompletedDate.ShouldBeNull();

        actionGroup.UpdateActionGroupState();

        actionGroup.State.ShouldBe(ActionGroupState.Completed);
        actionGroup.CompletedDate.ShouldNotBeNull();
        actionGroup.CompletedDate.Value.Date.ShouldBe(DateTime.Today);
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