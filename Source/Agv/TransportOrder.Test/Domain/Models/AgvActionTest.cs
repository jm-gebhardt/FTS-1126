using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Test.Helper;
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

        action.SetTarget(target.ToGraphNode());

        action.Target.Node.ShouldNotBeNull();
        action.Target.Node.Name.ShouldBe(target);
        action.IsIncomplete.ShouldBeFalse();
    }

    [TestMethod]
    public void SetTarget_ActionIsNotIncompleteWithoutValidation_DoesNotThrow()
    {
        var action = AgvActionGenerator.GenerateAction(Guid.NewGuid(), "target");

        Should.NotThrow(() => action.SetTarget("new target".ToGraphNode()));
    }

    [TestMethod]
    public void SkipAction_ValidData_ActionSkipped()
    {
        var action = AgvActionGenerator.GenerateAction(Guid.NewGuid(), "target");

        action.Skip();

        action.ShouldSkip.ShouldBeTrue();
    }

    [TestMethod]
    public void Equals_IdenticalAction_ReturnsTrue()
    {
        var action = new TransportOrder.Domain.Models.ActionGroup.AgvAction(Guid.NewGuid(), ActionType.Drop, new ActionTarget(null), Guid.NewGuid());
        var other = new TransportOrder.Domain.Models.ActionGroup.AgvAction(action.CorrelationId, ActionType.Drop, new ActionTarget(null), action.ParentId) { CreatedDate = action.CreatedDate };
        action.Equals(other).ShouldBeTrue();
    }

    [TestMethod]
    public void HasDockFront_DockFrontSet_ReturnsTrue()
    {
        var action = new TransportOrder.Domain.Models.ActionGroup.AgvAction(Guid.NewGuid(), ActionType.Drop, new ActionTarget(null), Guid.NewGuid());
        action.SetDockFrontValue(true);

        action.HasDockFront().ShouldBeTrue();
    }

    [TestMethod]
    public void HasDockFront_DockFrontNotSet_ReturnsFalse()
    {
        var action = new TransportOrder.Domain.Models.ActionGroup.AgvAction(Guid.NewGuid(), ActionType.Drop, new ActionTarget(null), Guid.NewGuid());

        action.HasDockFront().ShouldBeFalse();
    }

    [TestMethod]
    public void SetLiftHeight_LiftHeightInAdditionalInfo_LiftHeightSet()
    {
        const string liftHeight = "0.9";
        var action = new TransportOrder.Domain.Models.ActionGroup.AgvAction(Guid.NewGuid(), ActionType.Drop, new ActionTarget(null), Guid.NewGuid())
        {
            AdditionalInfo = $"LiftHeight:{liftHeight}"
        };
        action.SetExternalLiftHeight();
        action.InfoToAgv.ShouldNotBeNull();
        action.InfoToAgv[InfoToAgvKey.LiftHeight].ShouldBe(liftHeight);
    }

    [TestMethod]
    public void SetLiftHeight_InfoToAgvExists_LiftHeightNotInAdditionalInfo_LiftHeightNotSet()
    {
        var action = new TransportOrder.Domain.Models.ActionGroup.AgvAction(Guid.NewGuid(), ActionType.Drop, new ActionTarget(null), Guid.NewGuid())
        {
            AdditionalInfo = "Some other info"
        };
        action.SetDockFrontValue(true);
        action.SetExternalLiftHeight();
        action.InfoToAgv.ShouldNotBeNull();
        action.InfoToAgv.TryGetValue(InfoToAgvKey.LiftHeight, out string? liftHeight);
        liftHeight.ShouldBeNull();
    }

    [TestMethod]
    public void SetLiftHeight_InfoToAgvDoesNotExist_LiftHeightNotInAdditionalInfo_LiftHeightNotSet()
    {
        var action = new TransportOrder.Domain.Models.ActionGroup.AgvAction(Guid.NewGuid(), ActionType.Drop, new ActionTarget(null), Guid.NewGuid())
        {
            AdditionalInfo = "Some other info"
        };
        action.SetExternalLiftHeight();
        action.InfoToAgv.ShouldBeNull();
    }

    [TestMethod]
    public void GetSap_ValidData_ReturnsSapValue()
    {
        const string sap = "1020";
        var action = new TransportOrder.Domain.Models.ActionGroup.AgvAction(Guid.NewGuid(), ActionType.Drop, new ActionTarget(null), Guid.NewGuid())
        {
            AdditionalInfo = $"SAP:{sap}"
        };

        action.GetSap().ShouldBe(sap);
    }

    [TestMethod]
    public void GetSapSapNotSet_ReturnsNull()
    {
        var action = new TransportOrder.Domain.Models.ActionGroup.AgvAction(Guid.NewGuid(), ActionType.Drop, new ActionTarget(null), Guid.NewGuid())
        {
            AdditionalInfo = "Some other info"
        };
        action.GetSap().ShouldBeNull();
    }
}