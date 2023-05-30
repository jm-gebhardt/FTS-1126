using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.CommonTest.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Extensions;

[TestClass]
public class AgvActionExtensionsTest
{
    [TestMethod]
    public void UpdateActionState_WhenActionStateIsStartedAndDifferentFromPreviousState_SetStartedDate()
    {
        var now = DateTime.UtcNow;
        TimeProvider.Set(() => now);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionToUpdateState = actionGroup.Actions.First();
        const ActionState newState = ActionState.Started;
        actionToUpdateState.State = ActionState.Created;
        actionToUpdateState.StartedDate.ShouldBeNull();

        actionToUpdateState.UpdateActionState(newState);

        actionToUpdateState.State.ShouldBe(newState);
        actionToUpdateState.StartedDate.ShouldNotBeNull();
        actionToUpdateState.StartedDate.Value.ShouldBe(now);
    }

    [TestMethod]
    public void UpdateActionState_WhenActionStateIsStartedAndSameFromPreviousState_DoesNotSetStartedDate()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionToUpdateState = actionGroup.Actions.First();
        var previousStartedDate = DateTime.Today.AddDays(-10);
        const ActionState state = ActionState.Started;
        actionToUpdateState.State = state;
        actionToUpdateState.StartedDate = previousStartedDate;

        actionToUpdateState.UpdateActionState(state);

        actionToUpdateState.State.ShouldBe(state);
        actionToUpdateState.StartedDate.ShouldBe(previousStartedDate);
    }

    [TestMethod]
    public void UpdateActionState_WhenActionStateIsCompletedAndDifferentFromPreviousState_SetCompletedDate()
    {
        var now = DateTime.UtcNow;
        TimeProvider.Set(() => now);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionToUpdateState = actionGroup.Actions.First();
        const ActionState state = ActionState.Started;
        const ActionState newState = ActionState.Completed;
        actionToUpdateState.State = state;
        actionToUpdateState.CompletedDate.ShouldBeNull();

        actionToUpdateState.UpdateActionState(newState);

        actionToUpdateState.State.ShouldBe(newState);
        actionToUpdateState.CompletedDate.ShouldNotBeNull();
        actionToUpdateState.CompletedDate.Value.ShouldBe(now);
    }

    [TestMethod]
    public void UpdateActionState_WhenActionStateIsCompletedAndSameFromPreviousState_DoesNotSetCompletedDate()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionToUpdateState = actionGroup.Actions.First();
        var previousCompletedDate = DateTime.Today.AddDays(-10);
        const ActionState state = ActionState.Completed;
        actionToUpdateState.State = state;
        actionToUpdateState.CompletedDate = previousCompletedDate;

        actionToUpdateState.UpdateActionState(state);

        actionToUpdateState.State.ShouldBe(state);
        actionToUpdateState.CompletedDate.ShouldBe(previousCompletedDate);
    }
}