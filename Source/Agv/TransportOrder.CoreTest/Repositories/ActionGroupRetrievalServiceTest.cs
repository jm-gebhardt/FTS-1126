using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using TransportOrder.Domain.Models.Graph;
using TransportOrder.Domain.ReadModels;
using AgvActionGenerator = Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper.AgvActionGenerator;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

[TestClass]
public class ActionGroupRetrievalServiceTest
{
    private static Mapper GetMapper() => MappingHelper.GetMapper(new GraphProfile());
    private static async Task AddDefaultAgvGraph(ActionGroupRetrievalServiceTestFixture fixture, Guid? correlationId = null)
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var mapper = GetMapper();
        var efGraph = mapper.Map<EfGraph>(graph);
        await fixture.AddAsync(efGraph);
    }

    [TestMethod]
    public async Task GetActionGroupsWithTargetNodeTypeAsync_ValidData_GetsActionGroupReadModelEnrichedWithNodeTypes()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());


        List<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithTargetNodeTypeAsync(new List<ActionGroupState>(),null, null,1);

        ICollection<AgvActionReadModel> actions = actionGroupReadModel.Single().Actions;

        actions.Count.ShouldBe(actionGroup.Actions.Count);
        actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actions.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);
    }

    [TestMethod]
    public async Task GetActionGroupsWithTargetNodeTypeAsync_MultipleAgvGraphsUsingIdenticalNodes_GetsActionGroupReadModelEnrichedWithNodeTypes()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());
        await AddDefaultAgvGraph(fixture, "anotherGraph".ToGuid());


        List<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithTargetNodeTypeAsync(new List<ActionGroupState>(), null, null, 1);

        ICollection<AgvActionReadModel> actions = actionGroupReadModel.Single().Actions;

        actions.Count.ShouldBe(actionGroup.Actions.Count);
        actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actions.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);
    }

    [TestMethod]
    public async Task GetActionGroupsWithTargetNodeTypeAsync_FilterByState_NoActionGroupInFilteredState_ReturnsEmptyList()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        List<ActionGroupState> otherStates = Enum.GetValues<ActionGroupState>().Except(new List<ActionGroupState> { actionGroup.State }).ToList();

        List<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithTargetNodeTypeAsync(otherStates, null, null, 1);
        actionGroupReadModel.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetActionGroupsWithTargetNodeTypeAsync_FilterByFromDate_NoActionGroupCreatedAfterFromDate_ReturnsEmptyList()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        var fromDate = actionGroup.CreatedDate.AddDays(1);

        List<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithTargetNodeTypeAsync(new List<ActionGroupState>(), fromDate, null, 1);
        actionGroupReadModel.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetActionGroupsWithTargetNodeTypeAsync_FilterByToDate_NoActionGroupCreatedBeforeToDate_ReturnsEmptyList()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        var toDate = actionGroup.CreatedDate.AddDays(-1);

        List<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithTargetNodeTypeAsync(new List<ActionGroupState>(), null, toDate, 1);
        actionGroupReadModel.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetActionGroupsWithTargetNodeTypeAsync_LimitReturnValueCount_ReturnsNumberOfActionGroupsGivenByLimit()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        await fixture.AddRangeAsync(pickDropActionGroup, chargeActionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        const int limit = 1;
        List<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithTargetNodeTypeAsync(new List<ActionGroupState>(), null, null, limit);
        actionGroupReadModel.Count.ShouldBe(limit);
    }

    [TestMethod]
    public async Task GetActionGroupsWithTargetNodeTypeAsync_MultipleActionGroups_ReturnsOrderedActionGroups()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroupCreatedFirst = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionGroupCreatedSecond = AgvActionGenerator.GenerateChargeActionGroup();
        var actionGroupCreatedManually = AgvActionGenerator.GenerateMoveActionGroup(isManual:true);
       
        await fixture.AddRangeAsync(actionGroupCreatedFirst, actionGroupCreatedSecond, actionGroupCreatedManually);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        List<ActionGroupReadModel> actionGroupReadModel = await fixture.ActionGroupRetrievalService
            .GetActionGroupsWithTargetNodeTypeAsync(new List<ActionGroupState>(), null, null, 3);

        var orderedActionGroupCorrelationIds = new List<Guid> { actionGroupCreatedFirst.CorrelationId, 
            actionGroupCreatedSecond.CorrelationId, actionGroupCreatedManually.CorrelationId };
        actionGroupReadModel.Select(a => a.CorrelationId).ToList().ShouldBe(orderedActionGroupCorrelationIds);
    }

    [TestMethod]
    public async Task GetCurrentAssignedActionGroupsWithTargetNodeTypeAsync_ValidData_ReturnsActionGroupsByAgvCorrelationId()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agvCorrelationId = Guid.NewGuid();
        actionGroup.AssignedAgv = agvCorrelationId;
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());
    
        IReadOnlyDictionary<Guid, ActionGroupReadModel?> actionGroupsByAgvCorrelationId = await fixture.ActionGroupRetrievalService.GetCurrentAssignedActionGroupsWithTargetNodeTypeAsync(new List<Guid> { agvCorrelationId });
    
        var actualActionGroup = actionGroupsByAgvCorrelationId[agvCorrelationId]!;
        actualActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);
    
            actualActionGroup.Actions.Count.ShouldBe(actionGroup.Actions.Count);
        actualActionGroup.Actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actualActionGroup.Actions.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);
    
     }

    [TestMethod]
    public async Task EnrichActionGroupWithTargetNodeTypesAsync_ValidData_EnrichesActionGroupWithNodeType()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());

        var actionGroupWithNodeType = await fixture.ActionGroupRetrievalService.EnrichActionGroupWithTargetNodeTypesAsync(actionGroup);
        ICollection<AgvActionReadModel> actions = actionGroupWithNodeType.Actions;

        actions.Count.ShouldBe(actionGroup.Actions.Count);
        actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actions.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);
    }

    [TestMethod]

    public async Task GetActionGroupWithTargetNodeTypesAsync_ValidData_ReturnsActionGroupWithTargetNodeTypes()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await fixture.AddAsync(actionGroup);
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());


        var actualActionGroup = await fixture.ActionGroupRetrievalService
            .GetActionGroupWithTargetNodeTypesAsync(actionGroup.CorrelationId);

        actualActionGroup.ShouldNotBeNull();
        actualActionGroup.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actualActionGroup.Actions.Count.ShouldBe(actionGroup.Actions.Count);
        actualActionGroup.Actions.First().TargetNodeType.ShouldBe(AgvGraph.NodeType.Pick);
        actualActionGroup.Actions.Last().TargetNodeType.ShouldBe(AgvGraph.NodeType.Drop);
    }

    [TestMethod]

    public async Task GetActionGroupWithTargetNodeTypesAsync_UnknownActionGroup_ReturnsNull()
    {
        await using var fixture = new ActionGroupRetrievalServiceTestFixture();

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        await AddDefaultAgvGraph(fixture, "aGraph".ToGuid());


        var actualActionGroup = await fixture.ActionGroupRetrievalService
            .GetActionGroupWithTargetNodeTypesAsync(actionGroup.CorrelationId);

        actualActionGroup.ShouldBeNull();
    }
}