using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services;

[TestClass]
public class ActionGroupRegistryTest
{
    private static void VerifyActionGroup(ActionGroup actualActionGroup, ActionGroup expectedActionGroup)
    {
        actualActionGroup.ShouldNotBeNull();
        expectedActionGroup.ShouldNotBeNull();
        actualActionGroup.ShouldSatisfyAllConditions(
            a => a.CorrelationId.ShouldBe(expectedActionGroup.CorrelationId),
            a => a.AssignedAgv.ShouldBe(expectedActionGroup.AssignedAgv),
            a => a.State.ShouldBe(expectedActionGroup.State),
            a => a.OrderId.ShouldBe(expectedActionGroup.OrderId),
            a => a.CompletedDate.ShouldBe(expectedActionGroup.CompletedDate),
            a => a.CreatedDate.ShouldBe(expectedActionGroup.CreatedDate),
            a => a.StartedDate.ShouldBe(expectedActionGroup.StartedDate));

        for (int i = 0; i < actualActionGroup.Actions.Count; i++)
        {
            VerifyAgvAction(actualActionGroup.Actions.ElementAt(i), expectedActionGroup.Actions.ElementAt(i));
        }
    }

    private static void VerifyAgvAction(AgvAction actualAction, AgvAction expectedAction)
    {
        actualAction.ShouldSatisfyAllConditions(
            o => o.ActionType.ShouldBe(expectedAction.ActionType),
            o => o.CorrelationId.ShouldBe(expectedAction.CorrelationId),
            o => o.CreatedDate.ShouldBe(expectedAction.CreatedDate),
            o => o.Nodes.ShouldBe(expectedAction.Nodes),
            o => o.CompletedDate.ShouldBe(expectedAction.CompletedDate),
            o => o.Sequence.ShouldBe(expectedAction.Sequence),
            o => o.State.ShouldBe(expectedAction.State),
            o => o.TargetNodeId.ShouldBe(expectedAction.TargetNodeId),
            o => o.ParentId.ShouldBe(expectedAction.ParentId),
            o => o.StartedDate.ShouldBe(expectedAction.StartedDate)
        );
    }

    private static IEnumerable<ActionGroup> CreateInitialActionGroups(int totalItems, ActionGroupState actionGroupState = ActionGroupState.Created)
    {
        ActionGroupState[] actionGroupStates = Enum.GetValues<ActionGroupState>();
        var actionGroups = new List<ActionGroup>();

        for (int i = 0; i < totalItems; i++)
        {
            actionGroups.Add(AgvActionGenerator.GeneratePickDropActionGroup(actionGroupState));
        }

        IEnumerable<ActionGroupState> otherStatesForTestingExclusion = actionGroupStates.Except(new List<ActionGroupState> { actionGroupState });
        actionGroups.AddRange(CreateInitialActionGroups(otherStatesForTestingExclusion));
        return actionGroups;
    }

    private static IEnumerable<ActionGroup> CreateInitialActionGroups(IEnumerable<ActionGroupState> actionGroupStates)
    {
        return actionGroupStates.Select(actionGroupState => AgvActionGenerator.GeneratePickDropActionGroup(actionGroupState)).ToList();
    }

    [TestMethod]
    public async Task InsertAsync_WhenSuccessfullyAddedToRegistry_CanGetActionGroupById()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = new ActionGroupRegistryTestFixture();

        await fixture.ActionGroupRegistry.InsertAsync(actionGroup);
        var actionGroupInRegistry = await fixture.ActionGroupRegistry.GetActionGroupAsync(actionGroup.CorrelationId);

        actionGroupInRegistry.ShouldNotBeNull();
        actionGroupInRegistry.Actions.ShouldNotBeEmpty();
        VerifyActionGroup(actionGroupInRegistry, actionGroup);
    }

    [TestMethod]
    [DataRow(ActionGroupState.Created, 2, 10)]
    [DataRow(ActionGroupState.Created, 0, 10)]
    [DataRow(ActionGroupState.Created, 10, 0)]
    [DataRow(ActionGroupState.Created, 10, 2)]
    [DataRow(ActionGroupState.Started, 2, 10)]
    [DataRow(ActionGroupState.Started, 0, 10)]
    [DataRow(ActionGroupState.Started, 10, 0)]
    [DataRow(ActionGroupState.Started, 10, 2)]
    [DataRow(ActionGroupState.Completed, 2, 10)]
    [DataRow(ActionGroupState.Completed, 0, 10)]
    [DataRow(ActionGroupState.Completed, 10, 0)]
    [DataRow(ActionGroupState.Completed, 10, 2)]
    public async Task GetActionGroupsAsync_WhenFromAndToDateIsNull_ReturnActionGroupsWithStateSpecifiedAndTotalCountLessOrEqualToSetMaxItems(ActionGroupState actionGroupState, int maxItem, int totalItemsInRegistry)
    {
        IEnumerable<ActionGroup> actionGroups = CreateInitialActionGroups(totalItemsInRegistry, actionGroupState);
        var fixture = await new ActionGroupRegistryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRegistry.GetActionGroupsAsync(new List<ActionGroupState> { actionGroupState }, fromDate: null, toDate: null, maxItems: maxItem);

        foreach (var actionGroupInRegistry in actionGroupsResult)
        {
            actionGroupInRegistry.State.ShouldBe(actionGroupState);
            actionGroupInRegistry.Actions.ShouldNotBeEmpty();
        }

        actionGroupsResult.Count.ShouldBeLessThanOrEqualTo(maxItem);
    }

    [TestMethod]
    [DataRow(2, 10)]
    [DataRow(0, 10)]
    [DataRow(10, 0)]
    [DataRow(10, 2)]
    public async Task GetActionGroupsAsync_WhenThereAreActionGroupWithinDateRangeSpecified_ReturnActionGroupsAndTotalCountLessOrEqualToSetMaxItems(int maxItem, int totalItemsInRegistry)
    {
        IEnumerable<ActionGroup> actionGroups = CreateInitialActionGroups(totalItemsInRegistry);
        var fixture = await new ActionGroupRegistryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        var fromDate = DateTime.Today.AddDays(-1);
        var toDate = DateTime.Today.AddDays(1);

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRegistry.GetActionGroupsAsync(new List<ActionGroupState> { ActionGroupState.Created }, fromDate: fromDate, toDate: toDate, maxItems: maxItem);

        foreach (var actionGroupInRegistry in actionGroupsResult)
        {
            actionGroupInRegistry.CreatedDate.ShouldBeInRange(fromDate, toDate);
            actionGroupInRegistry.Actions.ShouldNotBeEmpty();
        }

        actionGroupsResult.Count.ShouldBeLessThanOrEqualTo(maxItem);
    }

    [TestMethod]
    [DataRow(2, 10)]
    [DataRow(0, 10)]
    [DataRow(10, 0)]
    [DataRow(10, 2)]
    public async Task GetActionGroupsAsync_WhenThereAreNoActionGroupWithinDateRangeSpecified_ReturnActionGroupsAndTotalCountLessOrEqualToSetMaxItems(int maxItem, int totalItemsInRegistry)
    {
        IEnumerable<ActionGroup> actionGroups = CreateInitialActionGroups(totalItemsInRegistry);
        var fixture = await new ActionGroupRegistryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        var fromDate = DateTime.Today.AddDays(-2);
        var toDate = DateTime.Today.AddDays(-1);

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRegistry.GetActionGroupsAsync(new List<ActionGroupState> { ActionGroupState.Created }, fromDate: fromDate, toDate: toDate, maxItems: maxItem);
        actionGroupsResult.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetActionGroupsAsync_WhenThereAreNoStateProvided_ReturnActionGroupsAllStates()
    {
        ActionGroupState[] actionGroupStates = Enum.GetValues<ActionGroupState>();
        IEnumerable<ActionGroup> actionGroups = CreateInitialActionGroups(actionGroupStates);
        var fixture = await new ActionGroupRegistryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRegistry.GetActionGroupsAsync(maxItems: 10);
        actionGroupsResult.ShouldNotBeEmpty();
        foreach (var actionGroupState in actionGroupStates)
        {
            actionGroupsResult.ShouldContain(x => x.State == actionGroupState);
        }
    }

    [TestMethod]
    [DataRow(ActionGroupState.Completed, 12)]
    [DataRow(ActionGroupState.Started, 12)]
    [DataRow(ActionGroupState.Created, 12)]
    [DataRow(ActionGroupState.Completed, 0)]
    public async Task GetActionGroupsCountAsync_WhenThereIsActionGroupForGivenState_ReturnCountOfActionGroups(ActionGroupState state, int expectedActionGroupCount)
    {
        IEnumerable<ActionGroup> actionGroups = CreateInitialActionGroups(expectedActionGroupCount, state);
        var fixture = await new ActionGroupRegistryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        int actualActionGroupCount = await fixture.ActionGroupRegistry.GetActionGroupsCountAsync(new[] { state });
        actualActionGroupCount.ShouldBe(expectedActionGroupCount);
    }

    [TestMethod]
    public async Task GetRequiredActionGroupByIdAsync_WhenThereIsNoActionForCorrelationIdProvided_ThrowActionGroupNotFoundException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = await new ActionGroupRegistryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        await Should.ThrowAsync<ActionGroupNotFoundException>(async () => await fixture.ActionGroupRegistry.GetRequiredActionGroupByIdAsync(Guid.NewGuid()));
        await Should.ThrowAsync<ActionGroupNotFoundException>(async () => await fixture.ActionGroupRegistry.GetRequiredActionGroupByIdAsync(Guid.Empty));
    }

    [TestMethod]
    public async Task GetRequiredActionGroupByIdAsync_WhenThereIsActionForCorrelationIdProvided_ReturnAction()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionToFetch = actionGroup.Actions.First();
        var fixture = await new ActionGroupRegistryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var actionInRegistry = await fixture.ActionGroupRegistry.GetRequiredActionByIdAsync(actionToFetch.CorrelationId);

        actionInRegistry.ShouldNotBeNull();
        VerifyAgvAction(actionInRegistry, actionToFetch);
    }

    [TestMethod]
    public async Task DeleteByParentIdAsync_WhenDeleteSuccessfully_ActionGroupNotExisting()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionToDeleteParentId = actionGroup.Actions.First();
        actionToDeleteParentId.ParentId.ShouldBe(actionGroup.CorrelationId);
        var fixture = await new ActionGroupRegistryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var actionGroupInRegistry = await fixture.ActionGroupRegistry.GetActionGroupAsync(actionGroup.CorrelationId);
        actionGroupInRegistry.ShouldNotBeNull();

        await fixture.ActionGroupRegistry.DeleteByParentIdAsync(actionToDeleteParentId.ParentId);

        actionGroupInRegistry = await fixture.ActionGroupRegistry.GetActionGroupAsync(actionGroup.CorrelationId);
        actionGroupInRegistry.ShouldBeNull();
    }

    [TestMethod]
    public async Task UpdateActionAndGroupStateAsync_WhenActionHasBeenStarted_Updates_ActionGroupState_ActionState_And_AssignedAgv()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionToUpdateState = actionGroup.Actions.First();
        actionToUpdateState.State = ActionState.Created;
        var fixture = await new ActionGroupRegistryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var actionGroupInRegistry = await fixture.ActionGroupRegistry.GetActionGroupAsync(actionGroup.CorrelationId);
        actionGroupInRegistry.ShouldNotBeNull();
        actionGroupInRegistry.State.ShouldBe(actionGroup.State);
        actionGroupInRegistry.Actions.First(a => a.CorrelationId == actionToUpdateState.CorrelationId).State.ShouldBe(actionToUpdateState.State);
        actionGroupInRegistry.AssignedAgv.ShouldBeNull();

        var actionGroupState = await fixture.ActionGroupRegistry.UpdateActionAndGroupStateAsync(actionToUpdateState, agv, ActionState.Started);
        actionGroupState.ShouldBe(ActionGroupState.Started);

        actionGroupInRegistry = await fixture.ActionGroupRegistry.GetActionGroupAsync(actionGroup.CorrelationId);
        actionGroupInRegistry.ShouldNotBeNull();
        actionGroupInRegistry.Actions.ShouldNotBeEmpty();
        actionGroupInRegistry.Actions.First(a => a.CorrelationId == actionToUpdateState.CorrelationId).State.ShouldBe(ActionState.Started);
        actionGroupInRegistry.State.ShouldBe(ActionGroupState.Started);
        actionGroupInRegistry.AssignedAgv.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_ReturnsNotStartedActionGroup()
    {
        const int totalItemsInRegistry = 10;
        IEnumerable<ActionGroup> actionGroups = CreateInitialActionGroups(totalItemsInRegistry);
        var fixture = await new ActionGroupRegistryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        IReadOnlyList<ActionGroup> notStartedActionGroups = await fixture.ActionGroupRegistry.GetNotStartedActionGroupsAsync();

        foreach (var notStartedActionGroup in notStartedActionGroups)
        {
            notStartedActionGroup.State.ShouldBe(ActionGroupState.Created);
            notStartedActionGroup.Actions.ShouldNotBeEmpty();
        }
        notStartedActionGroups.Count.ShouldBe(totalItemsInRegistry);
    }

    [TestMethod]
    public async Task GetSubsequentActionAsync_ReturnsSubsequentAction()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(ActionGroupState.Started);
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = agv.CorrelationId;

        var firstAction = actionGroup.Actions.First();
        firstAction.State = ActionState.Started;

        var subsequentAction = actionGroup.Actions.ElementAt(1);
        subsequentAction.State = ActionState.Created;

        var fixture = await new ActionGroupRegistryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var subsequentActionInRegistry = await fixture.ActionGroupRegistry.GetSubsequentActionAsync(agv.CorrelationId);
        VerifyAgvAction(subsequentActionInRegistry, subsequentAction);
    }

    [TestMethod]
    public async Task GetCurrentAssignedActionGroupAsync_WhenAgvProvidedHaveCurrentAssignedActionGroup_ReturnTupleWithCurrentAssignedActionGroup()
    {  
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(ActionGroupState.Started);
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = agv.CorrelationId;

        var fixture = await new ActionGroupRegistryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        IReadOnlyDictionary<Guid, ActionGroup?> currentAssignedActionGroupsAsync = await fixture.ActionGroupRegistry.GetCurrentAssignedActionGroupsAsync(new[] { agv.CorrelationId });
        currentAssignedActionGroupsAsync.Count.ShouldBe(1);
        KeyValuePair<Guid, ActionGroup?> currentActionGroup = currentAssignedActionGroupsAsync.First();
        currentActionGroup.Key.ShouldBe(agv.CorrelationId);
        currentActionGroup.Value.ShouldNotBeNull();
        VerifyActionGroup(currentActionGroup.Value, actionGroup);
    }
    
    [TestMethod]
    public async Task GetCurrentAssignedActionGroupAsync_WhenAgvProvidedHaveNoCurrentAssignedActionGroup_ReturnTupleWithNullValueForCurrentAssignedActionGroup()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(ActionGroupState.Started);
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = Guid.NewGuid();

        var fixture = await new ActionGroupRegistryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        IReadOnlyDictionary<Guid, ActionGroup?> currentAssignedActionGroup = await fixture.ActionGroupRegistry.GetCurrentAssignedActionGroupsAsync(new[] { agv.CorrelationId });
        currentAssignedActionGroup.Count.ShouldBe(1);
        currentAssignedActionGroup.First().Key.ShouldBe(agv.CorrelationId);
        currentAssignedActionGroup.First().Value.ShouldBeNull();
    }
}