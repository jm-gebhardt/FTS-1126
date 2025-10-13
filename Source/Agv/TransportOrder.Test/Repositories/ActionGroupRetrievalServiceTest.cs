using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Filter.Api;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Swagger.Examples;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;
using SortDirection = Gebhardt.Acc.Common.Filter.Enums.SortDirection;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class ActionGroupRetrievalServiceTest
{
    private static Mapper GetMapper()
    {
        return MappingHelper.GetMapper(new GraphProfile());
    }

    private static async Task AddDefaultAgvGraph(ActionGroupRetrievalServiceTestFixture fixture, Guid? correlationId = null)
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var mapper = GetMapper();
        var efGraph = mapper.Map<EfAgvGraph>(graph);
        await fixture.AddAsync(efGraph,
            context =>
            {
                foreach (var node in efGraph.Nodes)
                {
                    DbSet<EfNode> dbSet = context.Set<EfNode>();
                    if (!dbSet.Any(n => n.Name == node.Name))
                    {
                        dbSet.Add(node);
                    }
                    else
                    {
                        dbSet.Attach(node);
                    }
                }
                foreach (var edge in efGraph.Edges)
                {
                    DbSet<EfEdge> dbSet = context.Set<EfEdge>();
                    if (!dbSet.Any(n => n.Id == edge.Id))
                    {
                        dbSet.Add(edge);
                    }
                    else
                    {
                        dbSet.Attach(edge);
                    }
                }
            }
        );
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_ValidFilterParameter_GetsActionGroupReadModelEnrichedWithNodeTypes()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, null, 1);

        ICollection<AgvActionReadModel> actions = actionGroupReadModel.Single().Actions;

        actions.Count.ShouldBe(actionGroup.Actions.Count);
        actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actions.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_ActionGroupWithMoveToCoordinate_GetsActionGroupReadModelEnrichedWithNodeTypes()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await fixture.AddAsync(actionGroup);
        var moveToCoordinateActionGroup = AgvActionGenerator.GenerateMoveToCoordinateActionGroup();
        await fixture.AddAsync(moveToCoordinateActionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyCollection<ActionGroupReadModel> actionGroups = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, null, 2);

        ICollection<AgvActionReadModel> actionsWithTargetNodes = actionGroups.Single(a => a.CorrelationId == actionGroup.CorrelationId).Actions;
        actionsWithTargetNodes.Count.ShouldBe(actionGroup.Actions.Count);
        actionsWithTargetNodes.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actionsWithTargetNodes.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);

        ICollection<AgvActionReadModel> movesToCoordinates = actionGroups.Single(a => a.CorrelationId == moveToCoordinateActionGroup.CorrelationId).Actions;
        movesToCoordinates.Count.ShouldBe(moveToCoordinateActionGroup.Actions.Count);
        movesToCoordinates.First().TargetNodeName.ShouldBeEmpty();
        movesToCoordinates.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.WayPoint);
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_ActionGroupWithIncompleteAction_GetsActionGroupReadModelEnrichedWithNodeTypes()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget();
        await fixture.AddAsync(actionGroup);
        var moveToCoordinateActionGroup = AgvActionGenerator.GenerateMoveToCoordinateActionGroup();
        await fixture.AddAsync(moveToCoordinateActionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyCollection<ActionGroupReadModel> actionGroups = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, null, 2);

        ICollection<AgvActionReadModel> actionsWithTargetNodes = actionGroups.Single(a => a.CorrelationId == actionGroup.CorrelationId).Actions.OrderBy(a => a.Sequence).ToList();
        actionsWithTargetNodes.Count.ShouldBe(actionGroup.Actions.Count);
        actionsWithTargetNodes.First(a => a.ActionType == ActionType.Move).TargetNodeType.ShouldBe(AgvGraph.NodeType.WayPoint);
        actionsWithTargetNodes.First(a => a.ActionType == ActionType.Pick).TargetNodeType.ShouldBe(AgvGraph.NodeType.WayPoint);
        actionsWithTargetNodes.First(a => a.ActionType == ActionType.Drop).TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);

        ICollection<AgvActionReadModel> movesToCoordinates = actionGroups.Single(a => a.CorrelationId == moveToCoordinateActionGroup.CorrelationId).Actions;
        movesToCoordinates.Count.ShouldBe(moveToCoordinateActionGroup.Actions.Count);
        movesToCoordinates.First().TargetNodeName.ShouldBeEmpty();
        movesToCoordinates.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.WayPoint);
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_UnknownTargetNodeId_GetsActionGroupReadModelEnrichedWithWayPointType()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup("UnknownTargetNodeId");
        actionGroup.InvalidateActionGroup([], DateTimeOffset.Now);
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, null, 1);

        ICollection<AgvActionReadModel> actions = actionGroupReadModel.Single().Actions;

        actions.Count.ShouldBe(actionGroup.Actions.Count);
        actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.WayPoint);
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_NoActionsInActionGroup_GetsActionGroupReadModel()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = new ActionGroup();
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, null, 1);

        ICollection<AgvActionReadModel> actions = actionGroupReadModel.Single().Actions;
        actions.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_MultipleAgvGraphsUsingIdenticalNodes_GetsActionGroupReadModelEnrichedWithNodeTypes()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());
        await AddDefaultAgvGraph(fixture, "anotherGraph".ToGuid());

        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, null, 1);

        ICollection<AgvActionReadModel> actions = actionGroupReadModel.Single().Actions;

        actions.Count.ShouldBe(actionGroup.Actions.Count);
        actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actions.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_FilterByState_NoActionGroupInFilteredState_ReturnsEmptyList()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        List<ActionGroupState> otherStates = Enum.GetValues<ActionGroupState>().Except(new List<ActionGroupState> { actionGroup.State }).ToList();

        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(otherStates, null, null, 1);
        actionGroupReadModel.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_FilterByFromDate_NoActionGroupCreatedAfterFromDate_ReturnsEmptyList()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        var fromDate = actionGroup.CreatedDate.AddDays(1);

        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), fromDate, null, 1);
        actionGroupReadModel.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_FilterByToDate_NoActionGroupCreatedBeforeToDate_ReturnsEmptyList()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        var toDate = actionGroup.CreatedDate.AddDays(-1);

        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, toDate, 1);
        actionGroupReadModel.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_LimitReturnValueCount_ReturnsNumberOfActionGroupsGivenByLimit()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        await fixture.AddRangeAsync(pickDropActionGroup, chargeActionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        const int limit = 1;
        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, null, limit);
        actionGroupReadModel.Count.ShouldBe(limit);
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_MultipleActionGroupsDefaultSortedByAndSortingDirection_ReturnsSortedActionGroups()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroupCreatedFirst = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionGroupCreatedSecond = AgvActionGenerator.GenerateChargeActionGroup();
        var actionGroupCreatedManually = AgvActionGenerator.GenerateMoveActionGroup(isManual: true);

        await fixture.AddRangeAsync(actionGroupCreatedFirst, actionGroupCreatedSecond, actionGroupCreatedManually);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, null, 3);

        var orderedActionGroupCorrelationIds = new List<Guid>
        {
            actionGroupCreatedManually.CorrelationId,
            actionGroupCreatedSecond.CorrelationId,
            actionGroupCreatedFirst.CorrelationId
        };
        actionGroupReadModel.Select(a => a.CorrelationId).ToList().ShouldBe(orderedActionGroupCorrelationIds);
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_SortedByCreatedDateAscending_ReturnsSortedActionGroups()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroupCreatedFirst = AgvActionGenerator.GeneratePickDropActionGroup(createdDate: DateTime.UtcNow.AddDays(-1));
        var actionGroupCreatedSecond = AgvActionGenerator.GeneratePickDropActionGroup(createdDate: DateTime.UtcNow.AddDays(1));

        await fixture.AddRangeAsync(actionGroupCreatedFirst, actionGroupCreatedSecond);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, null, 3, sortBy: SortBy.CreatedDate, sortDescending: false);

        var orderedActionGroupCorrelationIds = new List<Guid>
        {
            actionGroupCreatedFirst.CorrelationId,
            actionGroupCreatedSecond.CorrelationId
        };
        actionGroupReadModel.Select(a => a.CorrelationId).ToList().ShouldBe(orderedActionGroupCorrelationIds);
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_SortedByCreatedDateDescending_ReturnsSortedActionGroups()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroupCreatedFirst = AgvActionGenerator.GeneratePickDropActionGroup(createdDate: DateTime.UtcNow.AddDays(-1));
        var actionGroupCreatedSecond = AgvActionGenerator.GeneratePickDropActionGroup(createdDate: DateTime.UtcNow.AddDays(1));

        await fixture.AddRangeAsync(actionGroupCreatedFirst, actionGroupCreatedSecond);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, null, 3, sortBy: SortBy.CreatedDate, sortDescending: true);

        var orderedActionGroupCorrelationIds = new List<Guid>
        {
            actionGroupCreatedSecond.CorrelationId,
            actionGroupCreatedFirst.CorrelationId
        };
        actionGroupReadModel.Select(a => a.CorrelationId).ToList().ShouldBe(orderedActionGroupCorrelationIds);
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_SortedByCompletedDateAscending_ReturnsSortedActionGroups()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroupCompletedFirst = AgvActionGenerator.GeneratePickDropActionGroup(completedDate: DateTime.UtcNow.AddDays(-1));
        var actionGroupCompletedSecond = AgvActionGenerator.GeneratePickDropActionGroup(completedDate: DateTime.UtcNow.AddDays(1));

        await fixture.AddRangeAsync(actionGroupCompletedFirst, actionGroupCompletedSecond);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, null, 3, sortBy: SortBy.CreatedDate, sortDescending: false);

        var orderedActionGroupCorrelationIds = new List<Guid>
        {
            actionGroupCompletedFirst.CorrelationId,
            actionGroupCompletedSecond.CorrelationId
        };
        actionGroupReadModel.Select(a => a.CorrelationId).ToList().ShouldBe(orderedActionGroupCorrelationIds);
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_SortedByCompletedDateDescending_ReturnsSortedActionGroups()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroupCompletedFirst = AgvActionGenerator.GeneratePickDropActionGroup(completedDate: DateTime.UtcNow.AddDays(-1));
        var actionGroupCompletedSecond = AgvActionGenerator.GeneratePickDropActionGroup(completedDate: DateTime.UtcNow.AddDays(1));

        await fixture.AddRangeAsync(actionGroupCompletedFirst, actionGroupCompletedSecond);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, null, 3, sortBy: SortBy.CreatedDate, sortDescending: true);

        var orderedActionGroupCorrelationIds = new List<Guid>
        {
            actionGroupCompletedSecond.CorrelationId,
            actionGroupCompletedFirst.CorrelationId
        };
        actionGroupReadModel.Select(a => a.CorrelationId).ToList().ShouldBe(orderedActionGroupCorrelationIds);
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_SortedByStartedDateAscending_ReturnsSortedActionGroups()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroupStartedFirst = AgvActionGenerator.GeneratePickDropActionGroup(startedDate: DateTime.UtcNow.AddDays(-1));
        var actionGroupStartedSecond = AgvActionGenerator.GeneratePickDropActionGroup(startedDate: DateTime.UtcNow.AddDays(1));

        await fixture.AddRangeAsync(actionGroupStartedFirst, actionGroupStartedSecond);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, null, 3, sortBy: SortBy.CreatedDate, sortDescending: false);

        var orderedActionGroupCorrelationIds = new List<Guid>
        {
            actionGroupStartedFirst.CorrelationId,
            actionGroupStartedSecond.CorrelationId
        };
        actionGroupReadModel.Select(a => a.CorrelationId).ToList().ShouldBe(orderedActionGroupCorrelationIds);
    }

    [TestMethod]
    public async Task GetActionGroupsWithNodesAsync_SortedByStartedDateDescending_ReturnsSortedActionGroups()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroupStartedFirst = AgvActionGenerator.GeneratePickDropActionGroup(startedDate: DateTime.UtcNow.AddDays(-1));
        var actionGroupStartedSecond = AgvActionGenerator.GeneratePickDropActionGroup(startedDate: DateTime.UtcNow.AddDays(1));

        await fixture.AddRangeAsync(actionGroupStartedFirst, actionGroupStartedSecond);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyCollection<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithNodesAsync(new List<ActionGroupState>(), null, null, 3, sortBy: SortBy.CreatedDate, sortDescending: true);

        var orderedActionGroupCorrelationIds = new List<Guid>
        {
            actionGroupStartedSecond.CorrelationId,
            actionGroupStartedFirst.CorrelationId
        };
        actionGroupReadModel.Select(a => a.CorrelationId).ToList().ShouldBe(orderedActionGroupCorrelationIds);
    }

    [TestMethod]
    public async Task GetCurrentActionGroupsWithNodesAsync_ValidData_ReturnsActionGroupsByAgvCorrelationId()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, [], [], DateTimeOffset.Now);
        var agvCorrelationId = Guid.NewGuid();
        actionGroup.AssignedAgv = agvCorrelationId;
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyDictionary<Guid, ActionGroupReadModel?> actionGroupsByAgvCorrelationId = await fixture.ActionGroupRetrievalService.GetCurrentActionGroupsWithNodesAsync(new List<Guid> { agvCorrelationId });

        var actualActionGroup = actionGroupsByAgvCorrelationId[agvCorrelationId]!;
        actualActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);

        actualActionGroup.Actions.Count.ShouldBe(actionGroup.Actions.Count);
        actualActionGroup.Actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actualActionGroup.Actions.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);
    }

    [TestMethod]
    public async Task GetCurrentActionGroupsWithNodesAsync_PausedActionGroups_ReturnsActionGroupsByAgvCorrelationId()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, [], [], DateTimeOffset.Now);
        actionGroup.PauseActionGroup(new Reason("reason"), DateTimeOffset.Now);
        var agvCorrelationId = Guid.NewGuid();
        actionGroup.AssignedAgv = agvCorrelationId;
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyDictionary<Guid, ActionGroupReadModel?> actionGroupsByAgvCorrelationId = await fixture.ActionGroupRetrievalService.GetCurrentActionGroupsWithNodesAsync(new List<Guid> { agvCorrelationId });

        var actualActionGroup = actionGroupsByAgvCorrelationId[agvCorrelationId]!;
        actualActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);

        actualActionGroup.Actions.Count.ShouldBe(actionGroup.Actions.Count);
        actualActionGroup.Actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actualActionGroup.Actions.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);
        actualActionGroup.State.ShouldBe(ActionGroupState.Paused);
    }

    [TestMethod]
    public async Task GetCurrentActionGroupsWithNodesAsync_ActionGroupWithMoveToCoordinate_ReturnsActionGroupsByAgvCorrelationId()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();
        var agvCorrelationId = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GenerateMoveToCoordinateActionGroup(assignedAgv: agvCorrelationId);
        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, [], [], DateTimeOffset.Now);
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyDictionary<Guid, ActionGroupReadModel?> actionGroupsByAgvCorrelationId = await fixture.ActionGroupRetrievalService.GetCurrentActionGroupsWithNodesAsync(new List<Guid> { agvCorrelationId });

        var actualActionGroup = actionGroupsByAgvCorrelationId[agvCorrelationId]!;
        actualActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);

        actualActionGroup.Actions.Count.ShouldBe(actionGroup.Actions.Count);
        actualActionGroup.Actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.WayPoint);
        actualActionGroup.Actions.Last().TargetNodeName.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetCurrentActionGroupsWithNodesAsync_UnknownTargetId_ReturnsActionGroupsByAgvCorrelationId()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup("UnknownTargetNodeId");
        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, [], [], DateTimeOffset.Now);
        var agvCorrelationId = Guid.NewGuid();
        actionGroup.AssignedAgv = agvCorrelationId;
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        IReadOnlyDictionary<Guid, ActionGroupReadModel?> actionGroupsByAgvCorrelationId = await fixture.ActionGroupRetrievalService.GetCurrentActionGroupsWithNodesAsync(new List<Guid> { agvCorrelationId });

        var actualActionGroup = actionGroupsByAgvCorrelationId[agvCorrelationId]!;
        actualActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);

        actualActionGroup.Actions.Count.ShouldBe(actionGroup.Actions.Count);
        actualActionGroup.Actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.WayPoint);
    }

    [TestMethod]
    public async Task EnrichActionGroupWithNodesAsync_ValidData_EnrichesActionGroupWithNodeType()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        var actionGroupWithNodeType = await fixture.ActionGroupRetrievalService.EnrichActionGroupWithNodesAsync(actionGroup);
        ICollection<AgvActionReadModel> actions = actionGroupWithNodeType.Actions;

        actions.Count.ShouldBe(actionGroup.Actions.Count);
        actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actions.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);
    }

    [TestMethod]
    public async Task GetActionGroupWithNodesAsync_GroupCorrelationId_ReturnsActionGroupWithTargetNodeTypes()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        var actualActionGroup = await fixture.ActionGroupRetrievalService
            .GetActionGroupWithNodesAsync(actionGroup.CorrelationId);

        actualActionGroup.ShouldNotBeNull();
        actualActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actualActionGroup.Actions.Count.ShouldBe(actionGroup.Actions.Count);
        actualActionGroup.Actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actualActionGroup.Actions.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);
    }

    [TestMethod]
    public async Task GetActionGroupWithNodesAsync_GroupCorrelationIdUnknownTargetNodeId_ReturnsActionGroupWithTargetNodeTypes()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup("UnknownTargetNodeId");
        actionGroup.InvalidateActionGroup(Enumerable.Empty<Reason>(), DateTimeOffset.Now);
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        var actualActionGroup = await fixture.ActionGroupRetrievalService
            .GetActionGroupWithNodesAsync(actionGroup.CorrelationId);

        actualActionGroup.ShouldNotBeNull();
        actualActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actualActionGroup.Actions.Count.ShouldBe(actionGroup.Actions.Count);
        actualActionGroup.Actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.WayPoint);
    }

    [TestMethod]
    public async Task GetActionGroupWithNodesAsync_UnknownActionGroup_ReturnsNull()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        var actualActionGroup = await fixture.ActionGroupRetrievalService
            .GetActionGroupWithNodesAsync(actionGroup.CorrelationId);

        actualActionGroup.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetCurrentAgvActionGroupWithNodesAsync_GroupCorrelationIdUnknownTargetNodeId_ReturnsActionGroupWithTargetNodeTypes()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup("UnknownTargetNodeId", assignedAgv: agv);
        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, [], [], DateTimeOffset.Now);
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        var actualActionGroup = await fixture.ActionGroupRetrievalService
            .GetCurrentAgvActionGroupWithNodesAsync(agv);

        actualActionGroup.ShouldNotBeNull();
        actualActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actualActionGroup.Actions.Count.ShouldBe(actionGroup.Actions.Count);
        actualActionGroup.Actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.WayPoint);
    }

    [TestMethod]
    public async Task GetCurrentAgvActionGroupWithNodesAsync_PausedActionGroup_ReturnsActionGroup()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        actionGroup.PauseActionGroup(new Reason("reason"), DateTimeOffset.Now);
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        var actualActionGroup = await fixture.ActionGroupRetrievalService
            .GetActionGroupWithNodesAsync(actionGroup.CorrelationId);

        actualActionGroup.ShouldNotBeNull();
        actualActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actualActionGroup.Actions.Count.ShouldBe(actionGroup.Actions.Count);
        actualActionGroup.Actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actualActionGroup.Actions.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);
        actualActionGroup.State.ShouldBe(ActionGroupState.Paused);
    }

    [TestMethod]
    public async Task GetCurrentAgvActionGroupWithNodesAsync_NoActionGroup_ReturnsNull()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        var actualActionGroup = await fixture.ActionGroupRetrievalService
            .GetCurrentAgvActionGroupWithNodesAsync(Guid.NewGuid());

        actualActionGroup.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetCurrentAgvActionGroupWithNodesAsync_NoActionGroupInStateStarted_ReturnsNull()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup("UnknownTargetNodeId", assignedAgv: agv);
        actionGroup.CompleteAction(actionGroup.Actions[0].CorrelationId, DateTimeOffset.Now);
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        var actualActionGroup = await fixture.ActionGroupRetrievalService
            .GetCurrentAgvActionGroupWithNodesAsync(agv);

        actualActionGroup.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetCurrentAgvActionGroupWithNodesAsync_ValidData_ReturnsActionGroupWithTargetNodeTypes()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();
        var agv = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv);
        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, [], [], DateTimeOffset.Now);
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        var actualActionGroup = await fixture.ActionGroupRetrievalService
            .GetCurrentAgvActionGroupWithNodesAsync(agv);

        actualActionGroup.ShouldNotBeNull();
        actualActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actualActionGroup.Actions.Count.ShouldBe(actionGroup.Actions.Count);
        actualActionGroup.Actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actualActionGroup.Actions.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);
    }

    [TestMethod]
    public async Task Filter_ValidFilter_FiltersActionGroups()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var agv = AgvGenerator.GenerateBusyAgv();

        var actionGroupCreatedFirst = AgvActionGenerator.GeneratePickDropActionGroup(createdDate: DateTime.UtcNow.AddDays(-1), completedDate: DateTime.UtcNow.AddHours(-23), assignedAgv: agv.CorrelationId);

        foreach (var a in actionGroupCreatedFirst.Actions)
        {
            actionGroupCreatedFirst.CompleteAction(a.CorrelationId, DateTimeOffset.UtcNow);
        }

        var actionGroupCreatedSecond = AgvActionGenerator.GeneratePickDropActionGroup(createdDate: DateTime.UtcNow.AddDays(1), assignedAgv: agv.CorrelationId);

        await fixture.AddAsync(agv);
        await fixture.AddRangeAsync(actionGroupCreatedFirst, actionGroupCreatedSecond);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        PagedResult<FilteredActionGroup> result = await fixture.ActionGroupRetrievalService.Filter(new CombinationFilter(
            LogicalCombinationVariant.And,
            [
                new ValueComparisonFilter<ActionGroupState>(nameof(ActionGroupFilterable.State), ComparisonVariant.Equals, ActionGroupState.Completed),
                new ValueComparisonFilter<DateTime>(nameof(ActionGroupFilterable.StartedDate), ComparisonVariant.Geq, DateTime.MinValue),
                new ValueComparisonFilter<Guid>(nameof(ActionGroupFilterable.AssignedAgv) + IFilter.AccessLevelSeparator + nameof(AgvFilterable.CorrelationId), ComparisonVariant.Equals, agv.CorrelationId)
            ]
        ), 0, 10, SortDirection.Desc, []);

        result.Items.Count.ShouldBe(1);
        result.PageSize.ShouldBe(10);
        result.CurrentPage.ShouldBe(0);
        result.TotalCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task Filter_ExampleFilter_FiltersActionGroups()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var agv = AgvGenerator.GenerateBusyAgv();

        var actionGroupCreatedFirst = AgvActionGenerator.GeneratePickDropActionGroup(createdDate: DateTime.UtcNow.AddDays(-1), completedDate: DateTime.UtcNow.AddHours(-23), assignedAgv: agv.CorrelationId);

        foreach (var a in actionGroupCreatedFirst.Actions)
        {
            actionGroupCreatedFirst.CompleteAction(a.CorrelationId, DateTimeOffset.UtcNow);
        }

        var actionGroupCreatedSecond = AgvActionGenerator.GeneratePickDropActionGroup(createdDate: DateTime.UtcNow.AddDays(1), assignedAgv: agv.CorrelationId);

        await fixture.AddAsync(agv);
        await fixture.AddRangeAsync(actionGroupCreatedFirst, actionGroupCreatedSecond);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());
        var filter = ActionGroupFilterExample.ExampleDto.Translate<ActionGroup, ActionGroupFilterable>();
        PagedResult<FilteredActionGroup> result = await fixture.ActionGroupRetrievalService.Filter(filter, 0, 10, SortDirection.Desc, []);

        result.Items.Count.ShouldBe(0);
        result.PageSize.ShouldBe(10);
        result.CurrentPage.ShouldBe(0);
        result.TotalCount.ShouldBe(0);
    }
}