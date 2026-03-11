
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Shouldly;
using AgvGraph = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.AgvGraph;
using AgvGraphGenerator = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv.AgvGraphGenerator;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

[TestClass]
public class OrderBuilderTest
{
    [TestMethod]
    public async Task Build_ValidData_ReturnsOrder()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), "agv-1");
        var pickAction = AgvGraphGenerator.GenerateNodeAction(Vda5050Specifications.ActionTypes.Pick, agvCorrelationId: agv.CorrelationId, RequirementType.Required, BlockingType.SOFT, [new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "Pick" }]);
        string[] nodeIds = ["map/NODE-10", "map/NODE-11", "PS01"];
        var agvGraph = AgvGraphGenerator.GenerateFromNodeIds(nodeIds, new Dictionary<string, ICollection<AgvNodeAction>> { { nodeIds.Last(), [pickAction] } }, agvCorrelationId: agv.CorrelationId);
        
        var action = AgvActionGenerator.GenerateAction(nodeIds.First(), ActionType.Pick, targetNodeId: nodeIds.Last());
        action.Edges = agvGraph.Edges.Select(e => e.Id).ToList();
        action.Nodes = nodeIds.Select(n => n.ToGraphNode()).ToList();
        var fixture = new OrderBuilderTestFixture()
            .AddActions([
                AgvGenerator.GenerateNodeAction(pickAction.ActionType,
                    agv.SerialNumber,
                    BlockingType.SOFT,
                    [
                        new AgvActionParameter
                        {
                            IsOptional = false,
                            Key = Vda5050Specifications.Parameters.StationType,
                            ValueDataType = ValueDataType.String
                        }
                    ])])
            .AddAgv(agv)
            .AgvAgvGraph(agvGraph);

        var order = await fixture.Builder.Build(agv.CorrelationId, action);

        order.OrderId.ShouldBe(action.CorrelationId.ToString());
        order.OrderUpdateId.ShouldBe(0);
        order.Route.IsEmpty.ShouldBeTrue();
        order.AssignedAgv.ShouldBe(agv);
        order.PlannedRoute.Nodes.Count.ShouldBe(nodeIds.Length);
        order.PlannedRoute.Edges.Count.ShouldBe(action.Edges.Count);
        order.PlannedRoute.Nodes.Single(n => n.NodeId == nodeIds.Last()).Actions.Single().ActionType.ShouldBe(Vda5050Specifications.ActionTypes.Pick);
    }

    [TestMethod]
    public async Task Build_EdgeAndNodeCountMismatch_ThrowsOrderNodeAndEdgeMismatchException()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), "agv-1");
        var pickAction = AgvGraphGenerator.GenerateNodeAction(Vda5050Specifications.ActionTypes.Pick, agvCorrelationId: agv.CorrelationId, RequirementType.Required, BlockingType.SOFT, [new OrderActionParameter { Key = Vda5050Specifications.Parameters.StationType, Value = "Pick" }]);
        string[] nodeIds = ["map/NODE-10", "map/NODE-11", "PS01"];
        var agvGraph = AgvGraphGenerator.GenerateFromNodeIds(nodeIds, new Dictionary<string, ICollection<AgvNodeAction>> { { nodeIds.Last(), [pickAction] } }, agvCorrelationId: agv.CorrelationId);

        var action = AgvActionGenerator.GenerateAction(nodeIds.First(), ActionType.Pick, targetNodeId: nodeIds.Last());
        action.Nodes = nodeIds.Select(n => n.ToGraphNode()).ToList();
        var fixture = new OrderBuilderTestFixture()
            .AddActions([
                AgvGenerator.GenerateNodeAction(pickAction.ActionType,
                    agv.SerialNumber,
                    BlockingType.SOFT,
                    [
                        new AgvActionParameter
                        {
                            IsOptional = false,
                            Key = Vda5050Specifications.Parameters.StationType,
                            ValueDataType = ValueDataType.String
                        }
                    ])])
            .AddAgv(agv)
            .AgvAgvGraph(agvGraph);

        await Should.ThrowAsync<OrderNodeAndEdgeMismatchException>(() => fixture.Builder.Build(agv.CorrelationId, action));
    }

    [TestMethod]
    public async Task Build_NodeNotInGraph_ThrowsNodeNotDefinedForAgvException()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), "agv-1");
        string[] nodeIds = ["map/NODE-10", "map/NODE-11"];
        var agvGraph = AgvGraphGenerator.GenerateFromNodeIds(nodeIds, new Dictionary<string, ICollection<AgvNodeAction>>(), agvCorrelationId: agv.CorrelationId);

        var action = AgvActionGenerator.GenerateAction(nodeIds.First(), targetNodeId: nodeIds.Last());
        action.Edges = agvGraph.Edges.Select(e => e.Id).ToList();
        List<GraphNode> unknownNodes = nodeIds.Select(n => n.ToGraphNode()).ToList();
        unknownNodes.Add("UNKNOWN-NODE".ToGraphNode());
        action.Nodes = unknownNodes;
        var fixture = new OrderBuilderTestFixture()
            .AddActions([])
            .AddAgv(agv)
            .AgvAgvGraph(agvGraph);

        await Should.ThrowAsync<NodeNotDefinedForAgvException>(() => fixture.Builder.Build(agv.CorrelationId, action));
    }

    [TestMethod]
    public async Task Build_EdgeNotInGraph_ThrowsEdgeNotDefinedForAgvException()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), "agv-1");
        string[] nodeIds = ["map/NODE-10", "map/NODE-11"];
        var agvGraph = AgvGraphGenerator.GenerateFromNodeIds(nodeIds, new Dictionary<string, ICollection<AgvNodeAction>>(), agvCorrelationId: agv.CorrelationId);

        var action = AgvActionGenerator.GenerateAction(nodeIds.First(), targetNodeId: nodeIds.Last());
        action.Nodes = nodeIds.Select(n => n.ToGraphNode()).ToList();
        List<Guid> unknownEdges = agvGraph.Edges.Select(e => e.Id).ToList();
        unknownEdges.Add(Guid.NewGuid());
        action.Edges = unknownEdges;
        var fixture = new OrderBuilderTestFixture()
            .AddActions([])
            .AddAgv(agv)
            .AgvAgvGraph(agvGraph);

        await Should.ThrowAsync<EdgeNotDefinedForAgvException>(() => fixture.Builder.Build(agv.CorrelationId, action));
    }

    [TestMethod]
    public async Task BuildNodes_WithOptionalActionMatchingActionType_AddsOptionalActionToNode()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), "agv-1");
        var pickAction = AgvGraphGenerator.GenerateNodeAction(Vda5050Specifications.ActionTypes.Pick, agvCorrelationId: agv.CorrelationId, RequirementType.Optional, BlockingType.SOFT);
        string[] nodeIds = ["map/NODE-10", "map/NODE-11", "PS01"];
        var agvGraph = AgvGraphGenerator.GenerateFromNodeIds(nodeIds, new Dictionary<string, ICollection<AgvNodeAction>> { { nodeIds.Last(), [pickAction] } }, agvCorrelationId: agv.CorrelationId);

        var action = AgvActionGenerator.GenerateAction(nodeIds.First(), ActionType.Pick, targetNodeId: nodeIds.Last());
        action.Edges = agvGraph.Edges.Select(e => e.Id).ToList();
        action.Nodes = nodeIds.Select(n => n.ToGraphNode()).ToList();
        var fixture = new OrderBuilderTestFixture()
            .AddActions([])
            .AddAgv(agv)
            .AgvAgvGraph(agvGraph);

        var order = await fixture.Builder.Build(agv.CorrelationId, action);

        order.PlannedRoute.Nodes.Single(n => n.NodeId == nodeIds.Last()).Actions.Single().ActionType.ShouldBe(Vda5050Specifications.ActionTypes.Pick);
    }

    [TestMethod]
    public async Task BuildNodes_WithOptionalActionNotMatchingActionType_DoesNotAddOptionalActionToNode()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), "agv-1");
        var pickAction = AgvGraphGenerator.GenerateNodeAction(Vda5050Specifications.ActionTypes.Pick, agvCorrelationId: agv.CorrelationId, RequirementType.Optional, BlockingType.SOFT);
        string[] nodeIds = ["map/NODE-10", "map/NODE-11", "PS01"];
        var agvGraph = AgvGraphGenerator.GenerateFromNodeIds(nodeIds, new Dictionary<string, ICollection<AgvNodeAction>> { { nodeIds.Last(), [pickAction] } }, agvCorrelationId: agv.CorrelationId);

        var action = AgvActionGenerator.GenerateAction(nodeIds.First(), targetNodeId: nodeIds.Last());
        action.Edges = agvGraph.Edges.Select(e => e.Id).ToList();
        action.Nodes = nodeIds.Select(n => n.ToGraphNode()).ToList();
        var fixture = new OrderBuilderTestFixture()
            .AddActions([])
            .AddAgv(agv)
            .AgvAgvGraph(agvGraph);

        var order = await fixture.Builder.Build(agv.CorrelationId, action);

        order.PlannedRoute.Nodes.Single(n => n.NodeId == nodeIds.Last()).Actions.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task BuildNodes_WithConditionalAction_AddsActionToNode()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), "agv-1");
        var conditionalAction = AgvGraphGenerator.GenerateNodeAction(Vda5050Specifications.ActionTypes.Pick, agvCorrelationId: agv.CorrelationId, RequirementType.Conditional, BlockingType.SOFT);
        string[] nodeIds = ["map/NODE-10", "map/NODE-11", "PS01"];
        var agvGraph = AgvGraphGenerator.GenerateFromNodeIds(nodeIds, new Dictionary<string, ICollection<AgvNodeAction>> { { nodeIds.Last(), [conditionalAction] } }, agvCorrelationId: agv.CorrelationId);

        var action = AgvActionGenerator.GenerateAction(nodeIds.First(), targetNodeId: nodeIds.Last());
        action.Edges = agvGraph.Edges.Select(e => e.Id).ToList();
        action.Nodes = nodeIds.Select(n => n.ToGraphNode()).ToList();
        var fixture = new OrderBuilderTestFixture()
            .AddActions([])
            .AddAgv(agv)
            .AgvAgvGraph(agvGraph);

        var order = await fixture.Builder.Build(agv.CorrelationId, action);

        order.PlannedRoute.Nodes.Single(n => n.NodeId == nodeIds.Last()).Actions.Single().ActionType.ShouldBe(Vda5050Specifications.ActionTypes.Pick);
    }

    [TestMethod]
    public async Task Build_SequenceIds_AreCorrectlyInterleaved()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), "agv-1");
        string[] nodeIds = ["map/NODE-10", "map/NODE-11", "PS01"];
        var agvGraph = AgvGraphGenerator.GenerateFromNodeIds(nodeIds, new Dictionary<string, ICollection<AgvNodeAction>>(), agvCorrelationId: agv.CorrelationId);

        var action = AgvActionGenerator.GenerateAction(nodeIds.First(), targetNodeId: nodeIds.Last());
        action.Edges = agvGraph.Edges.Select(e => e.Id).ToList();
        action.Nodes = nodeIds.Select(n => n.ToGraphNode()).ToList();
        var fixture = new OrderBuilderTestFixture()
            .AddActions([])
            .AddAgv(agv)
            .AgvAgvGraph(agvGraph);

        var order = await fixture.Builder.Build(agv.CorrelationId, action);

        List<OrderNode> nodes = order.PlannedRoute.Nodes.OrderBy(n => n.SequenceId).ToList();
        List<OrderEdge> edges = order.PlannedRoute.Edges.OrderBy(e => e.SequenceId).ToList();
        nodes[0].SequenceId.ShouldBe(0);
        edges[0].SequenceId.ShouldBe(1);
        nodes[1].SequenceId.ShouldBe(2);
        edges[1].SequenceId.ShouldBe(3);
        nodes[2].SequenceId.ShouldBe(4);
    }

    [TestMethod]
    public async Task BuildEdges_WithRequiredEdgeAction_AddsActionToEdge()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), "agv-1");
        var edgeAction = AgvGraphGenerator.GenerateEdgeAction(Vda5050Specifications.ActionTypes.WaitForTrigger, agv.CorrelationId, RequirementType.Required, BlockingType.SOFT);
        var node1 = AgvGraphGenerator.GenerateNode("map/NODE-10");
        var node2 = AgvGraphGenerator.GenerateNode("PS01");
        var edge = AgvGraphGenerator.GenerateEdge(node1.NodeId, node2.NodeId, [edgeAction]);
        var agvGraph = new AgvGraph(agv.CorrelationId, [node1, node2], [edge]);

        var action = AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01");
        action.Nodes = ["map/NODE-10".ToGraphNode(), "PS01".ToGraphNode()];
        action.Edges = [edge.Id];
        var fixture = new OrderBuilderTestFixture()
            .AddActions([])
            .AddAgv(agv)
            .AgvAgvGraph(agvGraph);

        var order = await fixture.Builder.Build(agv.CorrelationId, action);

        order.PlannedRoute.Edges.Single().Actions.Single().ActionType.ShouldBe(Vda5050Specifications.ActionTypes.WaitForTrigger);
    }

    [TestMethod]
    public async Task BuildEdges_WithOptionalEdgeAction_DoesNotAddActionToEdge()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), "agv-1");
        var edgeAction = AgvGraphGenerator.GenerateEdgeAction(Vda5050Specifications.ActionTypes.WaitForTrigger, agv.CorrelationId, RequirementType.Optional, BlockingType.SOFT);
        var node1 = AgvGraphGenerator.GenerateNode("map/NODE-10");
        var node2 = AgvGraphGenerator.GenerateNode("PS01");
        var edge = AgvGraphGenerator.GenerateEdge(node1.NodeId, node2.NodeId, [edgeAction]);
        var agvGraph = new AgvGraph(agv.CorrelationId, [node1, node2], [edge]);

        var action = AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01");
        action.Nodes = ["map/NODE-10".ToGraphNode(), "PS01".ToGraphNode()];
        action.Edges = [edge.Id];
        var fixture = new OrderBuilderTestFixture()
            .AddActions([])
            .AddAgv(agv)
            .AgvAgvGraph(agvGraph);

        var order = await fixture.Builder.Build(agv.CorrelationId, action);

        order.PlannedRoute.Edges.Single().Actions.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task BuildNodes_RequiredActionNotInFactSheet_BuildsFromGraphOnly()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), "agv-1");
        var pickAction = AgvGraphGenerator.GenerateNodeAction(Vda5050Specifications.ActionTypes.Pick, agvCorrelationId: agv.CorrelationId, RequirementType.Required, BlockingType.SOFT);
        string[] nodeIds = ["map/NODE-10", "map/NODE-11", "PS01"];
        var agvGraph = AgvGraphGenerator.GenerateFromNodeIds(nodeIds, new Dictionary<string, ICollection<AgvNodeAction>> { { nodeIds.Last(), [pickAction] } }, agvCorrelationId: agv.CorrelationId);

        var action = AgvActionGenerator.GenerateAction(nodeIds.First(), ActionType.Pick, targetNodeId: nodeIds.Last());
        action.Edges = agvGraph.Edges.Select(e => e.Id).ToList();
        action.Nodes = nodeIds.Select(n => n.ToGraphNode()).ToList();
        var fixture = new OrderBuilderTestFixture()
            .AddActions([]) // No fact sheet actions
            .AddAgv(agv)
            .AgvAgvGraph(agvGraph);

        var order = await fixture.Builder.Build(agv.CorrelationId, action);

        order.PlannedRoute.Nodes.Single(n => n.NodeId == nodeIds.Last()).Actions.Single().ActionType.ShouldBe(Vda5050Specifications.ActionTypes.Pick);
    }
}