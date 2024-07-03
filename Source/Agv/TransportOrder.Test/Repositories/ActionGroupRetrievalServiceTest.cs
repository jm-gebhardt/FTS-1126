using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

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
                    if (!dbSet.Any(n => n.Id == node.Id))
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
    public async Task GetActionGroupsWithNodesAsync_UnknownTargetNodeId_GetsActionGroupReadModelEnrichedWithWayPointType()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup("UnknownTargetNodeId");
        actionGroup.InvalidateActionGroup(Enumerable.Empty<Reason>());
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
        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, [], []);
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
    public async Task GetCurrentActionGroupsWithNodesAsync_UnknownTargetId_ReturnsActionGroupsByAgvCorrelationId()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup("UnknownTargetNodeId");
        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, [], []);
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
        actionGroup.InvalidateActionGroup(Enumerable.Empty<Reason>());
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
        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, [], []);
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
        actionGroup.CompleteAction(actionGroup.Actions[0].CorrelationId);
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
        actionGroup.StartAction(actionGroup.Actions[0].CorrelationId, [], []);
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
}