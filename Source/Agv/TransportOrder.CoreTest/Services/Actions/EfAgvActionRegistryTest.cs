using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions;

[TestClass]
public class EfAgvActionRegistryTest
{
    #region Setup
    private static void CompareActions(AgvAction? agvAction1, AgvAction? agvAction2)
    {
        agvAction1.ShouldNotBeNull();
        agvAction2.ShouldNotBeNull();

        agvAction1.CorrelationId.ShouldBe(agvAction2.CorrelationId);
        agvAction1.ParentId.ShouldBe(agvAction2.ParentId);
        agvAction1.Sequence.ShouldBe(agvAction2.Sequence);
        agvAction1.Successor.ShouldBe(agvAction2.Successor);
        agvAction1.Predecessor.ShouldBe(agvAction2.Predecessor);
        agvAction1.AgvId.ShouldBe(agvAction2.AgvId);
        agvAction1.AssignedAgv.ShouldBe(agvAction2.AssignedAgv);
        agvAction1.State.ShouldBe(agvAction2.State);
        agvAction1.ActionType.ShouldBe(agvAction2.ActionType);
        agvAction1.OrderId.ShouldBe(agvAction2.OrderId);
        agvAction1.TargetNodeId.ShouldBe(agvAction2.TargetNodeId);
        agvAction1.Nodes.ShouldBe(agvAction2.Nodes);
    }
    #endregion

    [TestMethod]
    public void Test_Insert_GetActionById()
    {
        var fixture = new EfAgvActionRegistryTestFixture();
        List<AgvAction> testActions = EfAgvActionRegistryTestFixture.CreateTestActions();

        var testAction = testActions[0];

        var agvAction = fixture.Registry.GetActionById(testAction.CorrelationId);
        agvAction.ShouldBeNull(); // Should not exist yet

        fixture.Registry.Insert(testAction);

        agvAction = fixture.Registry.GetActionById(testAction.CorrelationId);
        CompareActions(agvAction, testAction);
    }

    [TestMethod]
    public void Test_GetPendingActions()
    {
        var fixture = new EfAgvActionRegistryTestFixture();
        List<AgvAction> testActions = EfAgvActionRegistryTestFixture.CreateTestActions();
        fixture.InitTestData(testActions);

        List<AgvAction> pendingActions = fixture.Registry.GetPendingActions();

        for (int i = 0; i < pendingActions.Count; i++)
        {
            var testAction = testActions[i];
            var agvAction = pendingActions[i];
            CompareActions(agvAction, testAction);
        }
    }

    [TestMethod]
    public void GetActionsByParentId_WhenItemsExist_ShouldReturnAllOfThem()
    {
        var fixture = new EfAgvActionRegistryTestFixture();
        List<AgvAction> testActions = EfAgvActionRegistryTestFixture.CreateTestActions();
        fixture.InitTestData(testActions);

        var testAction = testActions[0];

        List<AgvAction> groupedActions = fixture.Registry.GetActionsByParentId(testAction.ParentId);
        groupedActions.ShouldNotBeNull();
        groupedActions.Count.ShouldBe(2);

        for (int i = 0; i < groupedActions.Count; i++)
        {
            testAction = testActions[i];
            var agvAction = groupedActions[i];
            CompareActions(agvAction, testAction);
        }
    }

    [TestMethod]
    public void UpdateState_WhenThereIsNoItemWithThisGuid_ShouldThrow()
    {
        var fixture = new EfAgvActionRegistryTestFixture();
        List<AgvAction> testActions = EfAgvActionRegistryTestFixture.CreateTestActions();
        fixture.InitTestData(testActions);

        Should.Throw<ActionUpdateFailedException>(() => fixture.Registry.UpdateState(Guid.Empty, ActionState.Completed));
    }

    [TestMethod]
    public void UpdateState_WhenItemExists_ShouldUpdateState()
    {
        var fixture = new EfAgvActionRegistryTestFixture();
        List<AgvAction> testActions = EfAgvActionRegistryTestFixture.CreateTestActions();
        fixture.InitTestData(testActions);

        fixture.Registry.UpdateState(testActions[0].CorrelationId, ActionState.Completed);

        var agvAction = fixture.Registry.GetActionById(testActions[0].CorrelationId);
        agvAction.ShouldNotBeNull();
        agvAction.State.ShouldBe(ActionState.Completed);
    }

    [TestMethod]
    public async Task GetSubsequentAction_WhenThereIsASubsequentAction_ShouldReturnSubsequentAction()
    {
        var fixture = new EfAgvActionRegistryTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var pickAction = actionGroup.Actions[0];
        var dropAction = actionGroup.Actions[1];
        var agv = AgvGenerator.GenerateIdleAgv();
        pickAction.AssignedAgv = agv.CorrelationId;
        pickAction.State = ActionState.Completed;
        dropAction.State = ActionState.Created;
        fixture.InitTestData(actionGroup.Actions);

        var subsequentAction = await fixture.Registry.GetSubsequentActionAsync(agv.CorrelationId);
        CompareActions(subsequentAction, dropAction);
    }

    [TestMethod]
    public void Delete_WhenDeletedByParentId_ShouldRemoveAction()
    {
        var fixture = new EfAgvActionRegistryTestFixture();
        List<AgvAction> testActions = EfAgvActionRegistryTestFixture.CreateTestActions();
        fixture.InitTestData(testActions);

        var testAction = testActions[0];

        fixture.Registry.DeleteByParentId(testAction.ParentId);

        var deletedAction = fixture.Registry.GetActionById(testAction.CorrelationId);
        deletedAction.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetSubsequentAction_WhenThereIsNoSubsequentAction_ThrowsActionNotFoundException()
    {
        var fixture = new EfAgvActionRegistryTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var pickAction = actionGroup.Actions[0];
        var dropAction = actionGroup.Actions[1];
        var agv = AgvGenerator.GenerateIdleAgv();
        pickAction.AssignedAgv = agv.CorrelationId;
        pickAction.State = ActionState.Completed;
        dropAction.State = ActionState.Started;

        fixture.InitTestData(actionGroup.Actions);

        await Should.ThrowAsync<ActionNotFoundException>(async () => await fixture.Registry.GetSubsequentActionAsync(agv.CorrelationId));
    }

    [TestMethod]
    public void GetNotStartedActionGroups_WhenThereAreNotStartedActionGroups_ReturnActionGroupsWithNotStartedActions()
    {
        var fixture = new EfAgvActionRegistryTestFixture();
        List<AgvAction> testActions = EfAgvActionRegistryTestFixture.CreateTestActionGroups();
        testActions[3].State = ActionState.Created;
        testActions[2].State = ActionState.Created;
        testActions[1].State = ActionState.Created;
        testActions[0].State = ActionState.Started;

        fixture.InitTestData(testActions);

        List<ActionGroup> notStartedActionGroup = fixture.Registry.GetNotStartedActionGroups();
        notStartedActionGroup.ShouldNotBeEmpty();
        IEnumerable<ActionGroup> startedActionGroup = notStartedActionGroup.Where(a => a.Actions.Any(action => action.State is ActionState.Started or ActionState.Completed));
        startedActionGroup.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetNotStartedActionGroups_WhenThereAreNoNotStartedActionGroups_ReturnNoActions()
    {
        var fixture = new EfAgvActionRegistryTestFixture();
        List<AgvAction> testActions = EfAgvActionRegistryTestFixture.CreateTestActionGroups();
        testActions[7].State = ActionState.Assigned;
        testActions[6].State = ActionState.Started;
        testActions[5].State = ActionState.Assigned;
        testActions[4].State = ActionState.Completed;
        testActions[3].State = ActionState.Started;
        testActions[2].State = ActionState.Created;
        testActions[1].State = ActionState.Created;
        testActions[0].State = ActionState.Started;

        fixture.InitTestData(testActions);

        List<ActionGroup> notStartedActionGroup = fixture.Registry.GetNotStartedActionGroups();
        notStartedActionGroup.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetUnfinishedActions_WhenThereArePendingActions_ReturnActions()
    {
        var fixture = new EfAgvActionRegistryTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var pickAction = actionGroup.Actions[0];
        var agv = AgvGenerator.GenerateIdleAgv();
        pickAction.AssignedAgv = agv.CorrelationId;
        pickAction.State = ActionState.Started;
        fixture.InitTestData(actionGroup.Actions);

        fixture.Registry.HasUnfinishedDropActions(agv.CorrelationId).ShouldBeTrue();
    }

    [TestMethod]
    [DataRow("AGV011")]
    [DataRow("NoDataAgv")]
    public void GetUnfinishedActions_WhenThereAreNoPendingActions_ReturnNoActions(string agvId)
    {
        var fixture = new EfAgvActionRegistryTestFixture();
        List<AgvAction> testActions = EfAgvActionRegistryTestFixture.CreateTestActionGroups();

        fixture.InitTestData(testActions);

        fixture.Registry.HasUnfinishedDropActions(agvId.ToGuid()).ShouldBeFalse();
    }
}