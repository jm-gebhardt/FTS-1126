using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class AgvActionTest
{
    [TestMethod]
    public void UpdateActionState_WhenActionStateIsStartedAndDifferentFromPreviousState_SetStartedDate()
    {
        var now = DateTime.UtcNow;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionToUpdateState = actionGroup.Actions.First();
        const ActionState newState = ActionState.Started;
        actionToUpdateState.StartedDate.ShouldBeNull();

        actionToUpdateState.UpdateActionState(newState);

        actionToUpdateState.State.ShouldBe(newState);
        actionToUpdateState.StartedDate.ShouldNotBeNull();
        actionToUpdateState.StartedDate.Value.ShouldBeInRange(now, DateTime.UtcNow);
    }

    [TestMethod]
    public void UpdateActionState_WhenActionStateIsStartedAndSameFromPreviousState_DoesNotSetStartedDate()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionToUpdateState = actionGroup.Actions.First();
        var previousStartedDate = DateTime.Today.AddDays(-10);
        const ActionState state = ActionState.Started;

        actionToUpdateState.UpdateActionState(state);

        actionToUpdateState.StartedDate = previousStartedDate;

        actionToUpdateState.UpdateActionState(state);

        actionToUpdateState.State.ShouldBe(state);
        actionToUpdateState.StartedDate.ShouldBe(previousStartedDate);
    }

    [TestMethod]
    public void UpdateActionState_WhenActionStateIsCompletedAndDifferentFromPreviousState_SetCompletedDate()
    {
        var now = DateTime.UtcNow;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionToUpdateState = actionGroup.Actions.First();
        const ActionState state = ActionState.Started;
        const ActionState newState = ActionState.Completed;

        actionToUpdateState.UpdateActionState(state);

        actionToUpdateState.CompletedDate.ShouldBeNull();

        actionToUpdateState.UpdateActionState(newState);

        actionToUpdateState.State.ShouldBe(newState);
        actionToUpdateState.CompletedDate.ShouldNotBeNull();
        actionToUpdateState.CompletedDate.Value.ShouldBeInRange(now, DateTime.UtcNow);
    }

    [TestMethod]
    public void UpdateActionState_WhenActionStateIsCompletedAndSameFromPreviousState_DoesNotSetCompletedDate()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionToUpdateState = actionGroup.Actions.First();
        var previousCompletedDate = DateTime.Today.AddDays(-10);
        const ActionState state = ActionState.Completed;
        actionToUpdateState.UpdateActionState(state);
        actionToUpdateState.CompletedDate = previousCompletedDate;

        actionToUpdateState.UpdateActionState(state);

        actionToUpdateState.State.ShouldBe(state);
        actionToUpdateState.CompletedDate.ShouldBe(previousCompletedDate);
    }

    [TestMethod]
    public void SetTarget_ValidData_TargetSet()
    {
        var action = AgvActionGenerator.GenerateAction(Guid.NewGuid(), string.Empty);
        const string target = "target";

        action.SetTarget(target);

        action.Target.NodeId.ShouldBe(target);
        action.IsIncomplete.ShouldBeFalse();
    }

    [TestMethod]
    public void SetTarget_ActionIsNotIncomplete_ThrowsSetActionTargetFailedException()
    {
        var action = AgvActionGenerator.GenerateAction(Guid.NewGuid(), "target");

        Should.Throw<SetActionTargetFailedException>(() => action.SetTarget("new target"));
    }
} 