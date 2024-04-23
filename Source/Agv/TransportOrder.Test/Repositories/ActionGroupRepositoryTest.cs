using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class ActionGroupRepositoryTest
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

    private static IEnumerable<ActionGroup> CreateInitialActionGroups(int totalItems, ActionGroupState actionGroupState = ActionGroupState.Created, Guid? assignedAgv = null)
    {
        ActionGroupState[] actionGroupStates = Enum.GetValues<ActionGroupState>();
        var actionGroups = new List<ActionGroup>();

        for (int i = 0; i < totalItems; i++)
        {
            actionGroups.Add(AgvActionGenerator.SetActionGroupToState(AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: assignedAgv), actionGroupState));
        }

        IEnumerable<ActionGroupState> otherStatesForTestingExclusion = actionGroupStates.Except(new List<ActionGroupState> { actionGroupState });
        actionGroups.AddRange(CreateInitialActionGroups(otherStatesForTestingExclusion));
        return actionGroups;
    }

    private static IEnumerable<ActionGroup> CreateInitialActionGroups(IEnumerable<ActionGroupState> actionGroupStates)
    {
        return actionGroupStates.Select(actionGroupState => AgvActionGenerator.SetActionGroupToState(AgvActionGenerator.GeneratePickDropActionGroup(), actionGroupState)).ToList();
    }

    [TestMethod]
    public async Task InsertAsync_WhenSuccessfullyAddedToRepository_CanGetActionGroupById()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = new ActionGroupRepositoryTestFixture();

        await fixture.ActionGroupRepository.AddAsync(actionGroup);
        var actionGroupInRepository = await fixture.ActionGroupRepository.GetActionGroupAsync(actionGroup.CorrelationId);

        actionGroupInRepository.ShouldNotBeNull();
        actionGroupInRepository.Actions.ShouldNotBeEmpty();
        VerifyActionGroup(actionGroupInRepository, actionGroup);
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
    public async Task GetActionGroupsAsync_WhenFromAndToDateIsNull_ReturnActionGroupsWithStateSpecifiedAndTotalCountLessOrEqualToSetMaxItems(ActionGroupState actionGroupState, int maxItem, int totalItemsInRepository)
    {
        IEnumerable<ActionGroup> actionGroups = CreateInitialActionGroups(totalItemsInRepository, actionGroupState);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRepository.GetActionGroupsAsync(new List<ActionGroupState> { actionGroupState }, null, null, maxItem);

        foreach (var actionGroupInRepository in actionGroupsResult)
        {
            actionGroupInRepository.State.ShouldBe(actionGroupState);
            actionGroupInRepository.Actions.ShouldNotBeEmpty();
        }

        actionGroupsResult.Count.ShouldBeLessThanOrEqualTo(maxItem);
    }

    [TestMethod]
    [DataRow(2, 10)]
    [DataRow(0, 10)]
    [DataRow(10, 0)]
    [DataRow(10, 2)]
    public async Task GetActionGroupsAsync_WhenThereAreActionGroupWithinDateRangeSpecified_ReturnActionGroupsAndTotalCountLessOrEqualToSetMaxItems(int maxItem, int totalItemsInRepository)
    {
        IEnumerable<ActionGroup> actionGroups = CreateInitialActionGroups(totalItemsInRepository);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        var fromDate = DateTime.Today.AddDays(-1);
        var toDate = DateTime.Today.AddDays(1);

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRepository.GetActionGroupsAsync(new List<ActionGroupState> { ActionGroupState.Created }, fromDate, toDate, maxItem);

        foreach (var actionGroupInRepository in actionGroupsResult)
        {
            actionGroupInRepository.CreatedDate.ShouldBeInRange(fromDate, toDate);
            actionGroupInRepository.Actions.ShouldNotBeEmpty();
        }

        actionGroupsResult.Count.ShouldBeLessThanOrEqualTo(maxItem);
    }

    [TestMethod]
    [DataRow(2, 10)]
    [DataRow(0, 10)]
    [DataRow(10, 0)]
    [DataRow(10, 2)]
    public async Task GetActionGroupsAsync_WhenThereAreNoActionGroupWithinDateRangeSpecified_ReturnActionGroupsAndTotalCountLessOrEqualToSetMaxItems(int maxItem, int totalItemsInRepository)
    {
        IEnumerable<ActionGroup> actionGroups = CreateInitialActionGroups(totalItemsInRepository);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        var fromDate = DateTime.Today.AddDays(-2);
        var toDate = DateTime.Today.AddDays(-1);

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRepository.GetActionGroupsAsync(new List<ActionGroupState> { ActionGroupState.Created }, fromDate, toDate, maxItem);
        actionGroupsResult.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetActionGroupsAsync_WhenThereAreNoStateProvided_ReturnActionGroupsAllStates()
    {
        ActionGroupState[] actionGroupStates = Enum.GetValues<ActionGroupState>();
        IEnumerable<ActionGroup> actionGroups = CreateInitialActionGroups(actionGroupStates);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRepository.GetActionGroupsAsync(maxItems: 10);
        actionGroupsResult.ShouldNotBeEmpty();
        foreach (var actionGroupState in actionGroupStates)
        {
            actionGroupsResult.ShouldContain(x => x.State == actionGroupState);
        }
    }

    [TestMethod]
    public async Task GetActionGroupsAsync_WhenGettingManualTriggeredActionGroupOnly_ReturnActionGroupsTriggeredManually()
    {
        List<ActionGroup> actionGroups = CreateInitialActionGroups(10).ToList();
        var moveActionGroupManual = AgvActionGenerator.GenerateMoveActionGroup("Target Node Id", true);
        actionGroups.Add(moveActionGroupManual);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        var fromDate = DateTime.Today.AddDays(-1);
        var toDate = DateTime.Today.AddDays(1);

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRepository.GetActionGroupsAsync(ActionGroupState.Created, fromDate, toDate, 5, isManual: true);
        foreach (var actionGroupInRepository in actionGroupsResult)
        {
            actionGroupInRepository.CreatedDate.ShouldBeInRange(fromDate, toDate);
            actionGroupInRepository.Actions.ShouldNotBeEmpty();
        }

        VerifyActionGroup(actionGroupsResult.Single(), moveActionGroupManual);
    }

    [TestMethod]
    public async Task GetActionGroupsAsync_SortByCreatedDateAscending_ReturnsSortedActionGroups()
    {
        var earlierCreatedGroup = AgvActionGenerator.GeneratePickDropActionGroup(createdDate: DateTime.UtcNow.AddDays(-1));
        var laterCreatedGroup = AgvActionGenerator.GeneratePickDropActionGroup(createdDate: DateTime.UtcNow.AddDays(1));

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(new List<ActionGroup> { earlierCreatedGroup, laterCreatedGroup })
            .BuildAsync();

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRepository.GetActionGroupsAsync(sortBy: SortBy.CreatedDate, sortDescending: false);
        actionGroupsResult.First().ShouldBeEquivalentTo(earlierCreatedGroup);
        actionGroupsResult.Last().ShouldBeEquivalentTo(laterCreatedGroup);
    }

    [TestMethod]
    public async Task GetActionGroupsAsync_SortByCreatedDateDescending_ReturnsSortedActionGroups()
    {
        var earlierCreatedGroup = AgvActionGenerator.GeneratePickDropActionGroup(createdDate: DateTime.UtcNow.AddDays(-1));
        var laterCreatedGroup = AgvActionGenerator.GeneratePickDropActionGroup(createdDate: DateTime.UtcNow.AddDays(1));

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(new List<ActionGroup> { earlierCreatedGroup, laterCreatedGroup })
            .BuildAsync();

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRepository.GetActionGroupsAsync(sortBy: SortBy.CreatedDate, sortDescending: true);
        actionGroupsResult.First().ShouldBeEquivalentTo(laterCreatedGroup);
        actionGroupsResult.Last().ShouldBeEquivalentTo(earlierCreatedGroup);
    }

    [TestMethod]
    public async Task GetActionGroupsAsync_SortByCompletedDateAscending_ReturnsSortedActionGroups()
    {
        var earlierCompletedGroup = AgvActionGenerator.GeneratePickDropActionGroup(completedDate: DateTime.UtcNow.AddDays(-1));
        var laterCompletedGroup = AgvActionGenerator.GeneratePickDropActionGroup(completedDate: DateTime.UtcNow.AddDays(1));

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(new List<ActionGroup> { earlierCompletedGroup, laterCompletedGroup })
            .BuildAsync();

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRepository.GetActionGroupsAsync(sortBy: SortBy.CompletedDate, sortDescending: false);
        actionGroupsResult.First().ShouldBeEquivalentTo(earlierCompletedGroup);
        actionGroupsResult.Last().ShouldBeEquivalentTo(laterCompletedGroup);
    }

    [TestMethod]
    public async Task GetActionGroupsAsync_SortByCompletedDateDescending_ReturnsSortedActionGroups()
    {
        var earlierCompletedGroup = AgvActionGenerator.GeneratePickDropActionGroup(completedDate: DateTime.UtcNow.AddDays(-1));
        var laterCompletedGroup = AgvActionGenerator.GeneratePickDropActionGroup(completedDate: DateTime.UtcNow.AddDays(1));

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(new List<ActionGroup> { earlierCompletedGroup, laterCompletedGroup })
            .BuildAsync();

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRepository.GetActionGroupsAsync(sortBy: SortBy.CompletedDate, sortDescending: true);
        actionGroupsResult.First().ShouldBeEquivalentTo(laterCompletedGroup);
        actionGroupsResult.Last().ShouldBeEquivalentTo(earlierCompletedGroup);
    }

    [TestMethod]
    public async Task GetActionGroupsAsync_SortByStartedDateAscending_ReturnsSortedActionGroups()
    {
        var earlierStartedGroup = AgvActionGenerator.GeneratePickDropActionGroup(startedDate: DateTime.UtcNow.AddDays(-1));
        var laterStartedGroup = AgvActionGenerator.GeneratePickDropActionGroup(startedDate: DateTime.UtcNow.AddDays(1));

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(new List<ActionGroup> { earlierStartedGroup, laterStartedGroup })
            .BuildAsync();

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRepository.GetActionGroupsAsync(sortBy: SortBy.StartedDate, sortDescending: false);
        actionGroupsResult.First().ShouldBeEquivalentTo(earlierStartedGroup);
        actionGroupsResult.Last().ShouldBeEquivalentTo(laterStartedGroup);
    }

    [TestMethod]
    public async Task GetActionGroupsAsync_SortByStartedDateDescending_ReturnsSortedActionGroups()
    {
        var earlierStartedGroup = AgvActionGenerator.GeneratePickDropActionGroup(startedDate: DateTime.UtcNow.AddDays(-1));
        var laterStartedGroup = AgvActionGenerator.GeneratePickDropActionGroup(startedDate: DateTime.UtcNow.AddDays(1));

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(new List<ActionGroup> { earlierStartedGroup, laterStartedGroup })
            .BuildAsync();

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRepository.GetActionGroupsAsync(sortBy: SortBy.StartedDate, sortDescending: true);
        actionGroupsResult.First().ShouldBeEquivalentTo(laterStartedGroup);
        actionGroupsResult.Last().ShouldBeEquivalentTo(earlierStartedGroup);
    }

    [TestMethod]
    [DataRow(ActionGroupState.Completed, 12)]
    [DataRow(ActionGroupState.Started, 18)]
    [DataRow(ActionGroupState.Created, 18)]
    [DataRow(ActionGroupState.Completed, 0)]
    public async Task GetActionGroupsCountAsync_WhenThereIsActionGroupForGivenState_ReturnCountOfActionGroups(ActionGroupState state, int expectedActionGroupCount)
    {
        IEnumerable<ActionGroup> actionGroups = CreateInitialActionGroups(expectedActionGroupCount, state);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        int actualActionGroupCount = await fixture.ActionGroupRepository.GetActionGroupsCountAsync(new[] { state });
        actualActionGroupCount.ShouldBe(expectedActionGroupCount);
    }

    [TestMethod]
    public async Task GetRequiredActionGroupByIdAsync_WhenThereIsNoActionForCorrelationIdProvided_ThrowActionGroupNotFoundException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        await Should.ThrowAsync<ActionGroupNotFoundException>(async () => await fixture.ActionGroupRepository.GetRequiredActionGroupByIdAsync(Guid.NewGuid()));
        await Should.ThrowAsync<ActionGroupNotFoundException>(async () => await fixture.ActionGroupRepository.GetRequiredActionGroupByIdAsync(Guid.Empty));
    }

    [TestMethod]
    public async Task GetRequiredActionGroupByIdAsync_WhenThereIsActionForCorrelationIdProvided_ReturnAction()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionToFetch = actionGroup.Actions.First();
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var actionInRepository = await fixture.ActionGroupRepository.GetRequiredActionByIdAsync(actionToFetch.CorrelationId);

        actionInRepository.ShouldNotBeNull();
        VerifyAgvAction(actionInRepository, actionToFetch);
    }

    [TestMethod]
    public async Task UpdateActionAndGroupStateAsync_WhenActionHasBeenStarted_Updates_ActionGroupState_ActionState_And_AssignedAgv()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionToUpdateState = actionGroup.Actions.First();
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var actionGroupInRepository = await fixture.ActionGroupRepository.GetActionGroupAsync(actionGroup.CorrelationId);
        actionGroupInRepository.ShouldNotBeNull();
        actionGroupInRepository.State.ShouldBe(actionGroup.State);
        actionGroupInRepository.Actions.First(a => a.CorrelationId == actionToUpdateState.CorrelationId).State.ShouldBe(actionToUpdateState.State);
        actionGroupInRepository.AssignedAgv.ShouldBeNull();

        var updatedActionGroup = await fixture.ActionGroupRepository.UpdateActionAndGroupStateAsync(actionToUpdateState, agv, ActionState.Started);

        actionGroupInRepository = await fixture.ActionGroupRepository.GetActionGroupAsync(actionGroup.CorrelationId);
        actionGroupInRepository.ShouldNotBeNull();
        actionGroupInRepository.Actions.ShouldNotBeEmpty();
        actionGroupInRepository.Actions.First(a => a.CorrelationId == actionToUpdateState.CorrelationId).State.ShouldBe(ActionState.Started);
        actionGroupInRepository.State.ShouldBe(ActionGroupState.Started);
        actionGroupInRepository.AssignedAgv.ShouldBe(agv.CorrelationId);

        actionGroupInRepository.ShouldBeEquivalentTo(updatedActionGroup);
    }

    [TestMethod]
    public async Task UpdateActionAndGroupStateAsync_WhenNoAgvProvided_Updates_ActionGroupState_ActionState_And_DoesNotAssignAgv()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionToUpdateState = actionGroup.Actions.First();
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var actionGroupInRepository = await fixture.ActionGroupRepository.GetActionGroupAsync(actionGroup.CorrelationId);
        actionGroupInRepository.ShouldNotBeNull();
        actionGroupInRepository.State.ShouldBe(actionGroup.State);
        actionGroupInRepository.Actions.First(a => a.CorrelationId == actionToUpdateState.CorrelationId).State.ShouldBe(actionToUpdateState.State);
        actionGroupInRepository.AssignedAgv.ShouldBeNull();

        var updatedActionGroup = await fixture.ActionGroupRepository.UpdateActionAndGroupStateAsync(actionToUpdateState, null, ActionState.Started);

        actionGroupInRepository = await fixture.ActionGroupRepository.GetActionGroupAsync(actionGroup.CorrelationId);
        actionGroupInRepository.ShouldNotBeNull();
        actionGroupInRepository.Actions.ShouldNotBeEmpty();
        actionGroupInRepository.Actions.First(a => a.CorrelationId == actionToUpdateState.CorrelationId).State.ShouldBe(ActionState.Started);
        actionGroupInRepository.State.ShouldBe(ActionGroupState.Started);
        actionGroupInRepository.AssignedAgv.ShouldBeNull();

        actionGroupInRepository.ShouldBeEquivalentTo(updatedActionGroup);
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_ReturnsNotStartedActionGroup()
    {
        const int totalItemsInRepositoryWithStateCreated = 10;
        var agvCorrelationId = Guid.NewGuid();

        IEnumerable<ActionGroup> actionGroups = CreateInitialActionGroups(totalItemsInRepositoryWithStateCreated, assignedAgv: agvCorrelationId);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        IReadOnlyList<ActionGroup> notStartedActionGroups = await fixture.ActionGroupRepository.GetNotStartedActionGroupsAsync(agvCorrelationId);

        foreach (var notStartedActionGroup in notStartedActionGroups)
        {
            notStartedActionGroup.State.ShouldBe(ActionGroupState.Created);
            notStartedActionGroup.Actions.ShouldNotBeEmpty();
        }

        notStartedActionGroups.Count.ShouldBe(totalItemsInRepositoryWithStateCreated);
    }

    [TestMethod]
    public async Task GetInvalidActionGroupAsync_ReturnsNotStartedActionGroup()
    {
        const int totalItemsInRepositoryWithStateInvalid = 5;

        IEnumerable<ActionGroup> actionGroups = CreateInitialActionGroups(totalItemsInRepositoryWithStateInvalid, ActionGroupState.Invalid);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        IReadOnlyList<ActionGroup> notStartedActionGroups = await fixture.ActionGroupRepository.GetInvalidActionGroupsAsync();

        foreach (var notStartedActionGroup in notStartedActionGroups)
        {
            notStartedActionGroup.State.ShouldBe(ActionGroupState.Invalid);
            notStartedActionGroup.Actions.ShouldNotBeEmpty();
        }

        notStartedActionGroups.Count.ShouldBe(totalItemsInRepositoryWithStateInvalid);
    }

    [TestMethod]
    public async Task GetSubsequentActionAsync_ReturnsSubsequentAction()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = agv.CorrelationId;

        actionGroup.StartAction(actionGroup.Actions.First().CorrelationId, ["source", "way", "target"]);
        actionGroup.CompleteAction(actionGroup.Actions.First().CorrelationId);

        var subsequentAction = actionGroup.Actions.ElementAt(1);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var subsequentActionInRepository = await fixture.ActionGroupRepository.GetSubsequentActionAsync(agv.CorrelationId);
        VerifyAgvAction(subsequentActionInRepository, subsequentAction);
    }

    [TestMethod]
    public async Task GetSubsequentActionAsync_NoCompletedAction_DoesNotReturnSubsequentAction()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = agv.CorrelationId;

        actionGroup.StartAction(actionGroup.Actions.First().CorrelationId, ["source", "way", "target"]);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        await Should.ThrowAsync<ActionNotFoundException>(fixture.ActionGroupRepository.GetSubsequentActionAsync(agv.CorrelationId));
    }

    [TestMethod]
    public async Task GetCurrentAssignedActionGroupAsync_WhenAgvProvidedHaveCurrentAssignedActionGroup_ReturnTupleWithCurrentAssignedActionGroup()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = agv.CorrelationId;

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        IReadOnlyDictionary<Guid, ActionGroup?> currentAssignedActionGroupsAsync = await fixture.ActionGroupRepository.GetCurrentAssignedActionGroupsAsync(new[] { agv.CorrelationId });
        currentAssignedActionGroupsAsync.Count.ShouldBe(1);
        KeyValuePair<Guid, ActionGroup?> currentActionGroup = currentAssignedActionGroupsAsync.First();
        currentActionGroup.Key.ShouldBe(agv.CorrelationId);
        currentActionGroup.Value.ShouldNotBeNull();
        VerifyActionGroup(currentActionGroup.Value, actionGroup);
    }

    [TestMethod]
    public async Task GetCurrentAssignedActionGroupAsync_WhenAgvProvidedHaveNoCurrentAssignedActionGroup_ReturnTupleWithNullValueForCurrentAssignedActionGroup()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = Guid.NewGuid();

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        IReadOnlyDictionary<Guid, ActionGroup?> currentAssignedActionGroup = await fixture.ActionGroupRepository.GetCurrentAssignedActionGroupsAsync(new[] { agv.CorrelationId });
        currentAssignedActionGroup.Count.ShouldBe(1);
        currentAssignedActionGroup.First().Key.ShouldBe(agv.CorrelationId);
        currentAssignedActionGroup.First().Value.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetCurrentAssignedActionGroupAsync_WhenRepositoryDoesNotHaveActionOfAgv_ReturnNull()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        var fixture = await new ActionGroupRepositoryTestFixture()
            .BuildAsync();

        var fetchedActionGroup = await fixture.ActionGroupRepository.GetCurrentAssignedActionGroupAsync(agv.CorrelationId);
        fetchedActionGroup.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetCurrentAssignedActionGroupsAsync_WhenRepositoryHasActionOfAgv_ReturnActionGroup()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = agv.CorrelationId;

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        IReadOnlyDictionary<Guid, ActionGroup?> currentAssignedActionGroup = await fixture.ActionGroupRepository.GetCurrentAssignedActionGroupsAsync(new[] { agv.CorrelationId });
        currentAssignedActionGroup.Count.ShouldBe(1);
        currentAssignedActionGroup.First().Key.ShouldBe(agv.CorrelationId);
        var fetchedActionGroup = currentAssignedActionGroup.First().Value;
        fetchedActionGroup.ShouldNotBeNull();
        fetchedActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);
    }

    [TestMethod]
    public async Task GetCurrentAssignedActionGroupAsync_WhenRepositoryHasActionOfAgv_ReturnActionGroup()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = agv.CorrelationId;

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var fetchedActionGroup = await fixture.ActionGroupRepository.GetCurrentAssignedActionGroupAsync(agv.CorrelationId);
        fetchedActionGroup.ShouldNotBeNull();
        fetchedActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);
    }

    [TestMethod]
    public async Task GetCurrentAgvActionAsync_WhenAgvHasCurrentAction_ReturnsCurrentAction()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = agv.CorrelationId;
        var currentAction = actionGroup.Actions.First();
        currentAction.UpdateActionState(ActionState.Started);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var currentAgvAction = await fixture.ActionGroupRepository.GetCurrentAgvActionAsync(agv.CorrelationId);
        currentAgvAction.ShouldNotBeNull();
        currentAgvAction.CorrelationId.ShouldBe(currentAction.CorrelationId);
    }

    [TestMethod]
    public async Task GetCurrentAgvActionAsync_WhenAgvHasNoCurrentAction_ReturnsNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = agv.CorrelationId;

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var currentAgvAction = await fixture.ActionGroupRepository.GetCurrentAgvActionAsync(agv.CorrelationId);
        currentAgvAction.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetRequiredActionGroupByActionIdAsync_ValidData_ReturnsActionGroup()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var actualActionGroup = await fixture.ActionGroupRepository.GetRequiredActionGroupByActionIdAsync(actionGroup.Actions.First().CorrelationId);

        actualActionGroup.ShouldBeEquivalentTo(actionGroup);
    }

    [TestMethod]
    public async Task GetRequiredActionGroupByActionIdAsync_Unknown_ThrowsActionGroupNotFoundException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        await Should.ThrowAsync<ActionGroupNotFoundException>(fixture.ActionGroupRepository.GetRequiredActionGroupByActionIdAsync("unknown".ToGuid()));
    }

    [TestMethod]
    public async Task HasStartedAction_AgvIsAssignedToActionGroupWithStartedAction_ReturnsTrue()
    {
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv, pickActionState: ActionState.Started);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();
        (await fixture.ActionGroupRepository.HasStartedActionAsync(agv)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task HasStartedAction_AgvIsNotAssignedToActionGroupWithStartedAction_ReturnsFalse()
    {
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: "another agv".ToGuid(), pickActionState: ActionState.Started);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();
        (await fixture.ActionGroupRepository.HasStartedActionAsync(agv)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task HasStartedAction_ActionGroupWithoutStartedAction_ReturnsFalse()
    {
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv, pickActionState: ActionState.Completed, dropActionState: ActionState.Created);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();
        (await fixture.ActionGroupRepository.HasStartedActionAsync(agv)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetAverageDurationEstimateDeviationAsync_ValidData_ReturnsAverageValueForPickDropActionGroupsWithGivenTargetNodes()
    {
        var agv = Guid.NewGuid();
        const double deviation = 10;
        var startedDate = DateTime.UtcNow;
        var completedDate = DateTime.UtcNow.AddSeconds(60);
        var duration = completedDate - startedDate - TimeSpan.FromSeconds(deviation);

        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv,
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Completed,
            startedDate: startedDate,
            completedDate: completedDate,
            duration: duration);
        var otherActionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([pickDropActionGroup, otherActionGroup])
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetAverageDurationEstimateDeviationAsync(agv, pickDropActionGroup.GetTargets())).ShouldBe(deviation);
    }

    [TestMethod]
    public async Task GetAverageDurationEstimateDeviationAsync_NoActionGroupsForThisAgv_ReturnsZero()
    {
        var agv = Guid.NewGuid();
        const double deviation = 10;
        var startedDate = DateTime.UtcNow;
        var completedDate = DateTime.UtcNow.AddSeconds(60);
        var duration = completedDate - startedDate - TimeSpan.FromSeconds(deviation);

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv,
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Completed,
            startedDate: startedDate,
            completedDate: completedDate,
            duration: duration);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetAverageDurationEstimateDeviationAsync("a different agv".ToGuid(), actionGroup.GetTargets())).ShouldBe(0);
    }

    [TestMethod]
    public async Task GetAverageDurationEstimateDeviationAsync_NoActionGroupWithTheseTargets_ReturnsZero()
    {
        var agv = Guid.NewGuid();
        const double deviation = 10;
        var startedDate = DateTime.UtcNow;
        var completedDate = DateTime.UtcNow.AddSeconds(60);
        var duration = completedDate - startedDate - TimeSpan.FromSeconds(deviation);

        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv,
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Completed,
            startedDate: startedDate,
            completedDate: completedDate,
            duration: duration);
        var otherActionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([pickDropActionGroup, otherActionGroup])
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetAverageDurationEstimateDeviationAsync(agv, ["a different pick node", "a different drop node"])).ShouldBe(0);
    }

    [TestMethod]
    public async Task GetAverageDurationEstimateDeviationAsync_NoCompletedActionGroupWithTheseTargets_ReturnsZero()
    {
        var agv = Guid.NewGuid();
        const double deviation = 10;
        var startedDate = DateTime.UtcNow;
        var completedDate = DateTime.UtcNow.AddSeconds(60);
        var duration = completedDate - startedDate - TimeSpan.FromSeconds(deviation);

        var uncompletedPickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv,
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Started,
            startedDate: startedDate,
            duration: duration);
        var otherActionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([uncompletedPickDropActionGroup, otherActionGroup])
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetAverageDurationEstimateDeviationAsync(agv, uncompletedPickDropActionGroup.GetTargets())).ShouldBe(0);
    }

    [TestMethod]
    public async Task GetActionDurationsByTypeAsync_NoActionGroupMatchingState_ReturnsEmptyDictionary()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: "source", destination: "target", pickActionState: ActionState.Completed, dropActionState: ActionState.Started);
        var actionGroupSameSourceAndTarget = AgvActionGenerator.GeneratePickDropActionGroup(source: "source", destination: "target", pickActionState: ActionState.Created, dropActionState: ActionState.Created);
        var actionGroupAnotherSource = AgvActionGenerator.GeneratePickDropActionGroup(source: "another source", destination: "target", pickActionState: ActionState.Started, dropActionState: ActionState.Created);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([actionGroup, actionGroupSameSourceAndTarget, actionGroupAnotherSource])
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetActionDurationsByTypeAsync(ActionGroupState.Completed, sourceNodeId: null, targetNodeId: "target")).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetActionDurationsByTypeAsync_SourceNotGiven_ReturnsActionDurationsOfGroupsMatchingStateAndTarget()
    {
        List<double> durations = Enumerable.Range(1, 3).ToList().Select(d => (double)d).ToList();
        List<ActionGroup> actionGroups = durations[..^1]
            .Select(duration => AgvActionGenerator.GeneratePickDropActionGroupWithActionDuration(source: "source",
                destination: "target",
                duration))
            .ToList();

        var actionGroupAnotherSource = AgvActionGenerator.GeneratePickDropActionGroupWithActionDuration(
            source: "another source",
            destination: "target",
            durations.Last());

        actionGroups.Add(actionGroupAnotherSource);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        Dictionary<ActionType, ICollection<double>> durationsByActionType = await fixture.ActionGroupRepository.GetActionDurationsByTypeAsync(ActionGroupState.Completed, sourceNodeId: null, targetNodeId: "target");
        durationsByActionType[ActionType.Pick].ShouldBeEquivalentTo(durations);
        durationsByActionType[ActionType.Drop].ShouldBeEquivalentTo(durations);
    }

    [TestMethod]
    public async Task GetActionDurationsByTypeAsync_TargetNotGiven_ReturnsActionDurationsOfGroupsMatchingStateAndSource()
    {
        List<double> durations = Enumerable.Range(1, 3).ToList().Select(d => (double)d).ToList();
        List<ActionGroup> actionGroups = durations[..^1]
            .Select(duration => AgvActionGenerator.GeneratePickDropActionGroupWithActionDuration(source: "source",
                destination: "target",
                duration))
            .ToList();

        var actionGroupAnotherTarget = AgvActionGenerator.GeneratePickDropActionGroupWithActionDuration(
            source: "source",
            destination: "another target",
            durations.Last());

        actionGroups.Add(actionGroupAnotherTarget);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        Dictionary<ActionType, ICollection<double>> durationsByActionType = await fixture.ActionGroupRepository.GetActionDurationsByTypeAsync(ActionGroupState.Completed, sourceNodeId: "source", targetNodeId: null);
        durationsByActionType[ActionType.Pick].ShouldBeEquivalentTo(durations);
        durationsByActionType[ActionType.Drop].ShouldBeEquivalentTo(durations);
    }

    [TestMethod]
    public async Task GetActionDurationsByTypeAsync_SourceAndTargetGiven_ReturnsActionDurationsOfGroupsMatchingStateSourceAndTarget()
    {
        List<double> durations = Enumerable.Range(1, 2).ToList().Select(d => (double)d).ToList();
        List<ActionGroup> actionGroups = durations
            .Select(duration => AgvActionGenerator.GeneratePickDropActionGroupWithActionDuration(source: "source",
                destination: "target",
                duration))
            .ToList();

        var actionGroupAnotherTarget = AgvActionGenerator.GeneratePickDropActionGroupWithActionDuration(
            source: "source",
            destination: "another target",
            10);

        actionGroups.Add(actionGroupAnotherTarget);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(actionGroups)
            .BuildAsync();

        Dictionary<ActionType, ICollection<double>> durationsByActionType = await fixture.ActionGroupRepository.GetActionDurationsByTypeAsync(ActionGroupState.Completed, sourceNodeId: "source", targetNodeId: "target");
        durationsByActionType[ActionType.Pick].ShouldBeEquivalentTo(durations);
        durationsByActionType[ActionType.Drop].ShouldBeEquivalentTo(durations);
    }
}