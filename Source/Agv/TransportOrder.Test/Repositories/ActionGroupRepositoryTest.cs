using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
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
            o => o.Target.NodeId.ShouldBe(expectedAction.Target.NodeId),
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
    public async Task AddOrUpdateAsync_WhenSuccessfullyAddedToRepository_CanGetActionGroupById()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = new ActionGroupRepositoryTestFixture();

        bool updated = await fixture.ActionGroupRepository.AddOrUpdateAsync(actionGroup);
        updated.ShouldBeFalse();
        var actionGroupInRepository = await fixture.ActionGroupRepository.GetActionGroupAsync(actionGroup.CorrelationId);

        actionGroupInRepository.ShouldNotBeNull();
        actionGroupInRepository.Actions.ShouldNotBeEmpty();
        VerifyActionGroup(actionGroupInRepository, actionGroup);
    }

    [TestMethod]
    public async Task AddOrUpdateAsync_WhenSuccessfullyUpdatedToRepository_CanGetUpdatedActionGroupById()
    {
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget();
        var fixture = new ActionGroupRepositoryTestFixture();

        bool updated = await fixture.ActionGroupRepository.AddOrUpdateAsync(actionGroup);
        updated.ShouldBeFalse();

        var updatedActionGroup = new ActionGroup(actionGroup.CorrelationId,
            actionGroup.OrderId,
            actionGroup.Actions.Where(a => a.ActionType != ActionType.Move).ToList(),
            actionGroup.Creator,
            true,
            actionGroup.CreatedDate,
            Guid.NewGuid());

        updated = await fixture.ActionGroupRepository.AddOrUpdateAsync(updatedActionGroup);
        updated.ShouldBeTrue();
        var actionGroupInRepository = await fixture.ActionGroupRepository.GetActionGroupAsync(actionGroup.CorrelationId);

        actionGroupInRepository.ShouldNotBeNull();
        actionGroupInRepository.Actions.Count.ShouldBe(updatedActionGroup.Actions.Count);
        actionGroupInRepository.IsQueued.ShouldBe(updatedActionGroup.IsQueued);
        VerifyActionGroup(actionGroupInRepository, updatedActionGroup);
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
    public async Task GetActionGroupsAsync_ProvideAgvAndState_ReturnActionGroup()
    {
        var agvCorrelationId = Guid.NewGuid();
        var actionGroupInCreatedState = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agvCorrelationId);
        var actionGroupInPausedState = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agvCorrelationId, pickActionState: ActionState.Paused);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups(new List<ActionGroup> { actionGroupInCreatedState, actionGroupInPausedState })
            .BuildAsync();

        IReadOnlyList<ActionGroup> actionGroupsResult = await fixture.ActionGroupRepository.GetActionGroupsAsync(actionGroupState: ActionGroupState.Paused, assignedAgvCorrelationId: agvCorrelationId, maxItems: 2);

        actionGroupsResult.Single().ShouldBeEquivalentTo(actionGroupInPausedState);
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
    public async Task GetRequiredActionGroupByIdAsync_ValidData_ReturnsActionGroup()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        actionGroup.Queue();
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroup.CorrelationId)).ShouldBeEquivalentTo(actionGroup);
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

        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, ["source", "way", "target"], []);
        actionGroup.CompleteAction(actionGroup.Actions[0].CorrelationId);

        var subsequentAction = actionGroup.Actions.ElementAt(1);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var subsequentActionInRepository = await fixture.ActionGroupRepository.GetSubsequentActionAsync(agv.CorrelationId);
        VerifyAgvAction(subsequentActionInRepository, subsequentAction);
    }

    [TestMethod]
    public async Task GetSubsequentActionAsync_NoCompletedAction_ReturnsFirstAction()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = agv.CorrelationId;

        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, ["source", "way", "target"], []);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var subsequentActionInRepository = await fixture.ActionGroupRepository.GetSubsequentActionAsync(agv.CorrelationId);
        VerifyAgvAction(subsequentActionInRepository, actionGroup.Actions.First());
    }

    [TestMethod]
    public async Task GetCurrentAssignedActionGroupAsync_WhenAgvProvidedHaveCurrentAssignedActionGroup_ReturnTupleWithCurrentAssignedActionGroup()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Started);
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
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Started);
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
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Started);
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
    public async Task GetCurrentAssignedActionGroupAsync_WithCompletedActionGroup_ReturnActionGroup()
    {
        var startedDate1 = DateTime.UtcNow.AddSeconds(-10);
        var startedDate2 = DateTime.UtcNow;
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(startedDate: startedDate1, assignedAgv: agv.CorrelationId,
            pickActionState: ActionState.Started);
        var actionGroup2 = AgvActionGenerator.GeneratePickDropActionGroup(startedDate: startedDate2, assignedAgv: agv.CorrelationId, 
            pickActionState: ActionState.Completed, 
            dropActionState: ActionState.Completed);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup1)
            .WithActionGroup(actionGroup2)
            .BuildAsync();

        var fetchedActionGroup = await fixture.ActionGroupRepository.GetCurrentAssignedActionGroupAsync(agv.CorrelationId);
        fetchedActionGroup.ShouldNotBeNull();
        fetchedActionGroup.CorrelationId.ShouldBe(actionGroup1.CorrelationId);
    }

    [TestMethod]
    public async Task GetCurrentAgvActionAsync_WhenAgvHasCurrentAction_ReturnsCurrentAction()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Started);
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = agv.CorrelationId;
        var currentAction = actionGroup.Actions.First();

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

    [TestMethod]
    public async Task GetNextManualActionGroupForAgvAsync_NoAllowedActionTypesOneManualActionGroup_ReturnsManualActionGroup()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.IsManual = true;

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetNextManualActionGroupForAgvAsync(actionGroup.AssignedAgv!.Value)).ShouldBeEquivalentTo(actionGroup);
    }

    [TestMethod]
    public async Task GetNextManualActionGroupForAgvAsync_MultipleManualActionGroups_ReturnsFirstCreatedManualActionGroup()
    {
        var agvCorrelationId = Guid.NewGuid();
        var firstCreatedActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agvCorrelationId, createdDate: DateTime.UtcNow);
        firstCreatedActionGroup.IsManual = true;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agvCorrelationId, createdDate: firstCreatedActionGroup.CreatedDate.AddSeconds(10));
        actionGroup.IsManual = true;

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([actionGroup, firstCreatedActionGroup])
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetNextManualActionGroupForAgvAsync(agvCorrelationId)).ShouldBeEquivalentTo(firstCreatedActionGroup);
    }

    [TestMethod]
    public async Task GetNextManualActionGroupForAgvAsync_NoManualActionGroups_ReturnsNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetNextManualActionGroupForAgvAsync(actionGroup.AssignedAgv!.Value)).ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNextManualActionGroupForAgvAsync_NoManualActionGroupsInStateCreated_ReturnsNull()
    {
        var agv = Guid.NewGuid();
        var startedActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv, startedDate: DateTime.UtcNow, pickActionState: ActionState.Started);
        startedActionGroup.IsManual = true;
        var completedActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv, completedDate: DateTime.UtcNow, pickActionState: ActionState.Completed, dropActionState: ActionState.Completed);
        completedActionGroup.IsManual = true;

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(startedActionGroup)
            .WithActionGroup(completedActionGroup)
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetNextManualActionGroupForAgvAsync(agv)).ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNextManualActionGroupForAgvAsync_NoManualActionGroupsForAgv_ReturnsNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.IsManual = true;

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetNextManualActionGroupForAgvAsync(Guid.NewGuid())).ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNextManualActionGroupForAgvAsync_NoManualActionGroupsWithAllowedActionTypes_ReturnsNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.IsManual = true;

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroup(actionGroup)
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetNextManualActionGroupForAgvAsync(actionGroup.AssignedAgv!.Value, [ActionType.Charge, ActionType.Move])).ShouldBeNull();
    }

    [TestMethod]
    public async Task IsLastCompletedActionMoveToCoordinate_LastActionIsMoveToCoordinate_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        var agvCorrelationId = Guid.NewGuid();
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agvCorrelationId, pickActionState: ActionState.Completed, dropActionState: ActionState.Completed, completedDate: now.AddMinutes(-3));
        var moveToCoordinateActionGroup = AgvActionGenerator.GenerateMoveToCoordinateActionGroup(assignedAgv: agvCorrelationId, moveActionState: ActionState.Completed, completedDate: now.AddMinutes(5));

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([pickDropActionGroup, moveToCoordinateActionGroup])
            .BuildAsync();

        (await fixture.ActionGroupRepository.IsLastCompletedActionMoveToCoordinate(agvCorrelationId)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task IsLastCompletedActionMoveToCoordinate_LastActionIsDropAtStation_ReturnsFalse()
    {
        var now = DateTime.UtcNow;
        var agvCorrelationId = Guid.NewGuid();
        var moveToCoordinateActionGroup = AgvActionGenerator.GenerateMoveToCoordinateActionGroup(assignedAgv: agvCorrelationId, moveActionState: ActionState.Completed, completedDate: now.AddMinutes(-5));
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agvCorrelationId, pickActionState: ActionState.Completed, dropActionState: ActionState.Completed, completedDate: now.AddMinutes(5));

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([pickDropActionGroup, moveToCoordinateActionGroup])
            .BuildAsync();

        (await fixture.ActionGroupRepository.IsLastCompletedActionMoveToCoordinate(agvCorrelationId)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetCurrentActionGroupsForStation_ValidData_ReturnsActionGroupsHeadingTowardsStationsAndActionGroupsThatReservedTheStation()
    {
        const string station = "a station";
        const string otherStation = "other station";
        
        var startedGroupPickTarget = AgvActionGenerator.GeneratePickDropActionGroup(source: station, pickActionState: ActionState.Started);
        var startedGroupDropTarget = AgvActionGenerator.GeneratePickDropActionGroup(destination: station, dropActionState: ActionState.Started);
        var pausedGroupPausedPickTarget = AgvActionGenerator.GeneratePickDropActionGroup(source: station, pickActionState: ActionState.Paused, destination: otherStation, dropActionState: ActionState.Created);
        var pausedGroupPausedDropTarget = AgvActionGenerator.GeneratePickDropActionGroup(source: otherStation, pickActionState: ActionState.Completed, destination: station, dropActionState: ActionState.Paused);
        var pausedGroupPausedOtherPickTarget = AgvActionGenerator.GeneratePickDropActionGroup(source: otherStation, pickActionState: ActionState.Paused, destination: station, dropActionState: ActionState.Created);
        var pausedGroupPausedOtherDropTarget = AgvActionGenerator.GeneratePickDropActionGroup(source: station, pickActionState: ActionState.Completed, destination: otherStation, dropActionState: ActionState.Paused);
        var createdGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: station);
        var startedGroupOtherTarget = AgvActionGenerator.GeneratePickDropActionGroup(source: otherStation, pickActionState: ActionState.Started);
        var chargeGroup = AgvActionGenerator.GenerateChargeActionGroup(targetNodeId: station);
        var parkGroup = AgvActionGenerator.GenerateMoveActionGroup(targetNodeId: station);
        var startedParkGroup = AgvActionGenerator.GenerateMoveActionGroup(targetNodeId: station);
        startedParkGroup.StartAction(startedParkGroup.Actions.First().CorrelationId, [], []);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([startedGroupPickTarget, startedGroupDropTarget, createdGroup, startedGroupOtherTarget, 
                               pausedGroupPausedPickTarget, pausedGroupPausedDropTarget, pausedGroupPausedOtherPickTarget, pausedGroupPausedOtherDropTarget,
                               chargeGroup, parkGroup, startedParkGroup])
            .BuildAsync();

        IReadOnlyCollection<ActionGroup> groupsForStation = await fixture.ActionGroupRepository.GetCurrentActionGroupsForStation(station);

        groupsForStation.Select(g => g.CorrelationId).ToList().ShouldBe(
        [
            startedGroupPickTarget.CorrelationId, startedGroupDropTarget.CorrelationId, // started pick-drop action groups where station is either source or destination
            pausedGroupPausedDropTarget.CorrelationId, pausedGroupPausedPickTarget.CorrelationId, pausedGroupPausedOtherPickTarget.CorrelationId, // paused pick-drop action groups where station is not completed
            chargeGroup.CorrelationId, parkGroup.CorrelationId, startedParkGroup.CorrelationId // charge and park action groups that reserved the station or are already started
        ], ignoreOrder: true);
    }

    [TestMethod]
    public async Task GetPausedActionGroupsForStation_ValidData_ReturnsPausedActionGroupsThatReservedTheStation()
    {
        const string station = "a station";
        var startedGroupPickTarget = AgvActionGenerator.GeneratePickDropActionGroup(source: station, pickActionState: ActionState.Started);
        var pausedGroupDropTarget = AgvActionGenerator.GeneratePickDropActionGroup(destination: station, dropActionState: ActionState.Paused);
        var createdGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: station);
        var pausedGroupOther = AgvActionGenerator.GeneratePickDropActionGroup(source: "to somewhere else", pickActionState: ActionState.Paused);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([startedGroupPickTarget, pausedGroupDropTarget, createdGroup, pausedGroupOther])
            .BuildAsync();

        IReadOnlyCollection<ActionGroup> groupsForStation = await fixture.ActionGroupRepository.GetPausedActionGroupsForStation(station);
        groupsForStation.Select(g => g.CorrelationId).ToList().ShouldBe(
        [
            pausedGroupDropTarget.CorrelationId
        ], ignoreOrder: true);
    }

    [TestMethod]
    public async Task GetDockFrontOfLastCompletedAction_PickDropActionGroup_ReturnsDockFrontIsNotChargeOrParkOrder()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Completed, completedDate: DateTime.UtcNow.AddMinutes(-10), assignedAgv: Guid.NewGuid());
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, null);
        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, null);
        bool dockFront = dropAction.InfoToAgv!.GetDockFront();

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([actionGroup])
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetDockFrontOfLastCompletedAction(actionGroup.AssignedAgv!.Value)).ShouldBe((dockFront, false));
    }

    [TestMethod]
    public async Task GetDockFrontOfLastCompletedAction_ParkActionGroup_ReturnsDockFrontIsChargeOrParkOrder()
    {
        var actionGroup = AgvActionGenerator.GenerateParkActionGroup(actionState: ActionState.Completed, completedDate: DateTime.UtcNow.AddMinutes(-10), assignedAgv: Guid.NewGuid());
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, null);
        bool dockFront = moveAction.InfoToAgv!.GetDockFront();

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([actionGroup])
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetDockFrontOfLastCompletedAction(actionGroup.AssignedAgv!.Value)).ShouldBe((dockFront, true));
    }

    [TestMethod]
    public async Task GetDockFrontOfLastCompletedAction_QueueGroup_ReturnsDockFrontIsNotChargeOrParkOrder()
    {
        var actionGroup = AgvActionGenerator.GenerateParkActionGroup(actionState: ActionState.Completed, completedDate: DateTime.UtcNow.AddMinutes(-10), assignedAgv: Guid.NewGuid());
        actionGroup.HasPriority = true;
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, null);
        bool dockFront = moveAction.InfoToAgv!.GetDockFront();

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([actionGroup])
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetDockFrontOfLastCompletedAction(actionGroup.AssignedAgv!.Value)).ShouldBe((dockFront, false));
    }

    [TestMethod]
    public async Task GetDockFrontOfLastCompletedAction_ChargeGroup_ReturnsDockFrontIsChargeOrParkOrder()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(actionStateMove: ActionState.Completed, actionStateCharge: ActionState.Completed, completedDate: DateTime.UtcNow.AddMinutes(-10), assignedAgv: Guid.NewGuid());
        var moveAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Move);
        var chargeAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Charge);

        actionGroup.SetDockFrontOfAction(moveAction.CorrelationId, null);
        actionGroup.SetDockFrontOfAction(chargeAction.CorrelationId, null);
        bool dockFront = chargeAction.InfoToAgv!.GetDockFront();

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([actionGroup])
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetDockFrontOfLastCompletedAction(actionGroup.AssignedAgv!.Value)).ShouldBe((dockFront, true));
    }

    [TestMethod]
    public async Task GetDockFrontOfLastCompletedAction_MultipleCompletedPickDropActionGroups_ReturnsDockFrontIsNotChargeOrParkOrder()
    {
        var assignedAgv = Guid.NewGuid();
        var earlierActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Completed, assignedAgv: assignedAgv);
        var earlierPickAction = earlierActionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        var earlierDropAction = earlierActionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        earlierActionGroup.SetDockFrontOfAction(earlierPickAction.CorrelationId, null);
        earlierActionGroup.SetDockFrontOfAction(earlierDropAction.CorrelationId, null);

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Completed, assignedAgv: assignedAgv);
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, earlierDropAction.InfoToAgv!.GetDockFront());
        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, earlierDropAction.InfoToAgv!.GetDockFront());
        bool dockFront = dropAction.InfoToAgv!.GetDockFront();

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([earlierActionGroup, actionGroup])
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetDockFrontOfLastCompletedAction(assignedAgv)).ShouldBe((dockFront, false));
    }

    [TestMethod]
    public async Task GetDockFrontOfLastCompletedAction_NoCompletedActionGroup_ReturnsNullIsNotChargeOrParkOrder()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Started, assignedAgv: Guid.NewGuid());
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, null);
        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, null);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([actionGroup])
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetDockFrontOfLastCompletedAction(actionGroup.AssignedAgv!.Value)).ShouldBe((null, false));
    }

    [TestMethod]
    public async Task GetDockFrontOfLastCompletedAction_NoLastActionGroup_ReturnsNullIsNotChargeOrParkOrder()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Started, assignedAgv: Guid.NewGuid());
        var pickAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Pick);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);

        actionGroup.SetDockFrontOfAction(pickAction.CorrelationId, null);
        actionGroup.SetDockFrontOfAction(dropAction.CorrelationId, null);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([actionGroup])
            .BuildAsync();

        (await fixture.ActionGroupRepository.GetDockFrontOfLastCompletedAction(Guid.NewGuid())).ShouldBe((null, false));
    }

    [TestMethod]
    public async Task GetRunningIdleActionGroups_ValidData_ReturnsRunningParkingAndChargingActionGroups()
    {
        var runningParkingActionGroup = AgvActionGenerator.GenerateParkActionGroup(actionState: ActionState.Started);
        var completedParkingActionGroup = AgvActionGenerator.GenerateParkActionGroup(actionState: ActionState.Completed);
        var createdParkingActionGroup = AgvActionGenerator.GenerateParkActionGroup(actionState: ActionState.Created);

        var runningChargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup(actionStateMove: ActionState.Started);
        var completedChargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup(actionStateMove: ActionState.Completed, actionStateCharge:ActionState.Completed);
        var createdChargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup(actionStateMove: ActionState.Created, actionStateCharge:ActionState.Created);

        var runningManualChargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup(actionStateMove: ActionState.Started);
        runningManualChargeActionGroup.IsManual = true;
        var runningPriorityChargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup(actionStateMove: ActionState.Started);
        runningPriorityChargeActionGroup.HasPriority = true;

        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Started);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([
                runningParkingActionGroup, completedParkingActionGroup, createdParkingActionGroup,
                runningChargeActionGroup, completedChargeActionGroup, createdChargeActionGroup,
                runningManualChargeActionGroup, runningPriorityChargeActionGroup, pickDropActionGroup
            ]).BuildAsync();

        (await fixture.ActionGroupRepository.GetRunningIdleActionGroups()).ShouldBeEquivalentTo(new List<ActionGroup>{runningParkingActionGroup, runningChargeActionGroup});
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public async Task AgvHasPendingActions_CreatedActionGroupWithoutAssignedAgv_ReturnsExpectedValue(bool isQueued)
    {
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: null, isQueued:isQueued);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([actionGroup]).BuildAsync();

        (await fixture.ActionGroupRepository.AgvHasPendingActions(agv)).ShouldBe(isQueued);
    }

    [TestMethod]
    public async Task AgvHasPendingActions_ActionGroupAssignedToDifferentAgv_ReturnsFalse()
    {
        var agv = Guid.NewGuid();
        var createdActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        var startedActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), pickActionState: ActionState.Started, dropActionState: ActionState.Created);
        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([createdActionGroup, startedActionGroup]).BuildAsync();

        (await fixture.ActionGroupRepository.AgvHasPendingActions(agv)).ShouldBeFalse();
    }

    [DataRow(ActionState.Started, ActionState.Created, true)]
    [DataRow(ActionState.Completed, ActionState.Started, false)]
    [DataRow(ActionState.Completed, ActionState.Completed, false)]
    [DataRow(ActionState.Created, ActionState.Created, true)]
    [DataTestMethod]
    public async Task AgvHasPendingActions_ActionGroupWithAssignedAgv_ReturnsExpectedValue(ActionState pickActionState, ActionState dropActionState, bool expected)
    {
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv, pickActionState: pickActionState, dropActionState: dropActionState);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([actionGroup]).BuildAsync();

        (await fixture.ActionGroupRepository.AgvHasPendingActions(agv)).ShouldBe(expected);
    }
        
    [DataRow(ActionState.Started)]
    [DataRow(ActionState.Created)]
    [DataTestMethod]
    public async Task FilterAgvsWithPriorityActionGroups_ValidData_ReturnsAgvsWithPriorityActionGroups(ActionState pickActionState)
    {
        var agvWithPriorityGroup = Guid.NewGuid();
        var agvWithNonPriorityGroup = Guid.NewGuid();
        var agvWithoutActionGroup = Guid.NewGuid();

        var priorityActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: pickActionState, assignedAgv: agvWithPriorityGroup);
        priorityActionGroup.HasPriority = true;
        var nonPriorityActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: pickActionState, assignedAgv: agvWithNonPriorityGroup);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([
                priorityActionGroup, nonPriorityActionGroup
            ]).BuildAsync();

        (await fixture.ActionGroupRepository.FilterAgvsWithPriorityActionGroups([agvWithPriorityGroup, agvWithoutActionGroup, agvWithNonPriorityGroup])).ShouldBeEquivalentTo(new List<Guid>{agvWithPriorityGroup});
    }

    [DataRow(ActionState.Aborted)]
    [DataRow(ActionState.Completed)]
    [DataRow(ActionState.Canceled)]
    [DataTestMethod]
    public async Task FilterAgvsWithPriorityActionGroups_NoPriorityActionGroupsInValidState_ReturnsAgvsWithPriorityActionGroups(ActionState actionState)
    {
        var agvWithPriorityGroup = Guid.NewGuid();
        var agvWithNonPriorityGroup = Guid.NewGuid();
        var agvWithoutActionGroup = Guid.NewGuid();

        var priorityActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: actionState, dropActionState:actionState, assignedAgv: agvWithPriorityGroup);
        priorityActionGroup.HasPriority = true;
        var nonPriorityActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Created, assignedAgv: agvWithNonPriorityGroup);

        var fixture = await new ActionGroupRepositoryTestFixture()
            .WithActionGroups([
                priorityActionGroup, nonPriorityActionGroup
            ]).BuildAsync();

        (await fixture.ActionGroupRepository.FilterAgvsWithPriorityActionGroups([agvWithPriorityGroup, agvWithoutActionGroup, agvWithNonPriorityGroup])).ShouldBeEmpty();
    }
}