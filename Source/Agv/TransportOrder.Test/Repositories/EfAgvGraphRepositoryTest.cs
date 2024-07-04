using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfAgvGraphRepositoryTest
{
    private static void VerifyAgvGraph(AgvGraph actualAgvGraph, AgvGraph expectedAgvGraph)
    {
        actualAgvGraph.ShouldNotBeNull();
        actualAgvGraph.ShouldSatisfyAllConditions(
            e => e.CorrelationId.ShouldBe(expectedAgvGraph.CorrelationId),
            e => e.Usability.ToString().ShouldBe(expectedAgvGraph.Usability.ToString()));
        actualAgvGraph.Nodes.Count.ShouldBe(expectedAgvGraph.Nodes.Count);
        actualAgvGraph.Edges.Count.ShouldBe(expectedAgvGraph.Edges.Count);

        for (int i = 0; i < actualAgvGraph.Nodes.Count; i++)
        {
            VerifyGraphNode(actualAgvGraph.Nodes.ElementAt(i), expectedAgvGraph.Nodes.ElementAt(i));
        }

        for (int i = 0; i < actualAgvGraph.Edges.Count; i++)
        {
            VerifyGraphEdge(actualAgvGraph.Edges.ElementAt(i), expectedAgvGraph.Edges.ElementAt(i));
        }
    }

    private static void VerifyGraphEdge(GraphEdge actualEdge, GraphEdge expectedEdge)
    {
        actualEdge.ShouldNotBeNull();
        actualEdge.ShouldSatisfyAllConditions(
            e => e.Id.ShouldBe(expectedEdge.Id),
            e => e.FromNodeId.ShouldBe(expectedEdge.FromNodeId),
            e => e.ToNodeId.ShouldBe(expectedEdge.ToNodeId),
            e => e.Length.ShouldBe(expectedEdge.Length));
    }

    private static void VerifyGraphNode(GraphNode actualNode, GraphNode expectedNode)
    {
        actualNode.ShouldNotBeNull();
        actualNode.ShouldSatisfyAllConditions(
            n => n.Id.ShouldBe(expectedNode.Id),
            n => n.Type.ToString().ShouldBe(expectedNode.Type.ToString()),
            n => n.X.ShouldBe(expectedNode.X),
            n => n.Y.ShouldBe(expectedNode.Y),
            n => n.Z.ShouldBe(expectedNode.Z),
            n => n.Theta.ShouldBe(expectedNode.Theta));
    }

    [TestMethod]
    public async Task GetNodesAsync_WhenAgvGraphIsExisting_ReturnsNodes()
    {
        var fixture = new EfAgvGraphRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var expectedAgvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        await fixture.Repository.UpdateGraphAsync(new Graph { Nodes = expectedAgvGraph.Nodes, Edges = expectedAgvGraph.Edges });
        await fixture.Repository.AddOrUpdateAgvGraphAsync(expectedAgvGraph);
        List<GraphNode> expectedNodes = expectedAgvGraph.Nodes.Where(x => x.Id.Contains('1')).ToList();

        List<GraphNode> nodes = (await fixture.Repository.GetNodesAsync(correlationId, node => node.Id.Contains('1'))).ToList();

        nodes.Count.ShouldBe(expectedNodes.Count);
        foreach (var node in nodes)
        {
            VerifyGraphNode(node, expectedNodes.First(x => x.Id == node.Id));
        }
    }

    [TestMethod]
    public async Task GetNodeAsync_WhenNodeExist_ReturnsNode()
    {
        var fixture = new EfAgvGraphRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var expectedAgvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        await fixture.Repository.UpdateGraphAsync(new Graph { Nodes = expectedAgvGraph.Nodes, Edges = expectedAgvGraph.Edges });
        await fixture.Repository.AddOrUpdateAgvGraphAsync(expectedAgvGraph);
        var expectedNode = expectedAgvGraph.Nodes.First();

        var actualNode = await fixture.Repository.GetNodeAsync(expectedNode.Id);

        VerifyGraphNode(actualNode, expectedNode);
    }

    [TestMethod]
    public async Task GetNodeAsync_WhenNodeIsNotFound_ThrowsNodeNotFoundException()
    {
        var fixture = new EfAgvGraphRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var expectedAgvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        await fixture.Repository.AddOrUpdateAgvGraphAsync(expectedAgvGraph);

        await Should.ThrowAsync<NodeNotFoundException>(async () => await fixture.Repository.GetNodeAsync("Not existing Node Id"));
    }

    [TestMethod]
    public async Task TryGetNodeAsync_WhenNodeExist_ReturnsNode()
    {
        var fixture = new EfAgvGraphRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var expectedAgvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        await fixture.Repository.UpdateGraphAsync(new Graph { Nodes = expectedAgvGraph.Nodes, Edges = expectedAgvGraph.Edges });
        await fixture.Repository.AddOrUpdateAgvGraphAsync(expectedAgvGraph);
        var expectedNode = expectedAgvGraph.Nodes.First();

        var actualNode = await fixture.Repository.TryGetNodeAsync(expectedNode.Id);

        VerifyGraphNode(actualNode!, expectedNode);
    }

    [TestMethod]
    public async Task TryGetNodeAsync_WhenNodeIsNotFound_ReturnsNull()
    {
        var fixture = new EfAgvGraphRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var expectedAgvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        await fixture.Repository.AddOrUpdateAgvGraphAsync(expectedAgvGraph);

        var actualNode = await fixture.Repository.TryGetNodeAsync("Not existing Node Id");

        actualNode.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAgvGraphAsync_WhenGraphIsNotExisting_ThrowsAgvGraphNotDefinedException()
    {
        var fixture = new EfAgvGraphRepositoryTestFixture();

        await Should.ThrowAsync<AgvGraphNotDefinedException>(async () => await fixture.Repository.GetAgvGraphAsync(Guid.NewGuid()));
    }

    [TestMethod]
    public async Task GetAgvGraphAsync_WhenGraphIsExisting_ReturnsAgvGraph()
    {
        var fixture = new EfAgvGraphRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var expectedAgvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        await fixture.Repository.UpdateGraphAsync(new Graph { Nodes = expectedAgvGraph.Nodes, Edges = expectedAgvGraph.Edges });
        await fixture.Repository.AddOrUpdateAgvGraphAsync(expectedAgvGraph);

        var agvGraphInRepository = await fixture.Repository.GetAgvGraphAsync(correlationId);

        VerifyAgvGraph(agvGraphInRepository, expectedAgvGraph);
    }

    [TestMethod]
    public async Task ExistsAsync_WhenEfGraphIsExisting_ReturnsTrue()
    {
        var fixture = new EfAgvGraphRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        await fixture.Repository.UpdateGraphAsync(new Graph { Nodes = agvGraph.Nodes, Edges = agvGraph.Edges });
        await fixture.Repository.AddOrUpdateAgvGraphAsync(agvGraph);

        bool isExisting = await fixture.Repository.ExistsAsync(correlationId);

        isExisting.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ExistsAsync_WHenEfGraphIsNotExisting_ReturnsFalse()
    {
        var fixture = new EfAgvGraphRepositoryTestFixture();

        bool isExisting = await fixture.Repository.ExistsAsync(Guid.NewGuid());

        isExisting.ShouldBeFalse();
    }

    [TestMethod]
    public async Task RemoveAsync_WhenAgvGraphIsNotExisting_ThrowsAgvGraphNotDefinedException()
    {
        var fixture = new EfAgvGraphRepositoryTestFixture();

        await Should.ThrowAsync<AgvGraphNotDefinedException>(async () => await fixture.Repository.RemoveAsync(Guid.NewGuid()));
    }

    [TestMethod]
    public async Task RemoveAsync_WhenAgvGraphIsExisting_DeletesSuccessfully()
    {
        var fixture = new EfAgvGraphRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var expectedAgvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        await fixture.Repository.UpdateGraphAsync(new Graph { Nodes = expectedAgvGraph.Nodes, Edges = expectedAgvGraph.Edges });
        await fixture.Repository.AddOrUpdateAgvGraphAsync(expectedAgvGraph);
        var agvGraphInRepository = await fixture.Repository.GetAgvGraphAsync(correlationId);
        agvGraphInRepository.ShouldNotBeNull();
        VerifyAgvGraph(agvGraphInRepository, expectedAgvGraph);

        await fixture.Repository.RemoveAsync(correlationId);

        await Should.ThrowAsync<AgvGraphNotDefinedException>(async () => await fixture.Repository.GetAgvGraphAsync(correlationId));
    }

    [TestMethod]
    public async Task GetSelectedAgvGraphsAsync_ValidData_ReturnsSelectedAgvGraphs()
    {
        var fixture = new EfAgvGraphRepositoryTestFixture();
        var agvGraphs = new List<AgvGraph>();

        for (int i = 0; i < 10; i++)
        {
            var agvGraph = AgvGraphGenerator.CreateDefaultGraph(Guid.NewGuid());
            agvGraphs.Add(agvGraph);
            await fixture.Repository.UpdateGraphAsync(new Graph { Nodes = agvGraph.Nodes, Edges = agvGraph.Edges });
            await fixture.Repository.AddOrUpdateAgvGraphAsync(agvGraph);
        }
        List<Guid> requestedGraphs = agvGraphs.Take(agvGraphs.Count - 1).Select(a => a.CorrelationId).ToList();
        ICollection<AgvGraph> agvGraphsInRepository = await fixture.Repository.GetSelectedAgvGraphsAsync(requestedGraphs);

        agvGraphsInRepository.Count.ShouldBe(requestedGraphs.Count);
        foreach (var agvGraphInRepository in agvGraphsInRepository)
        {
            var expectedAgvGraph = agvGraphs.First(x => x.CorrelationId == agvGraphInRepository.CorrelationId);
            VerifyAgvGraph(agvGraphInRepository, expectedAgvGraph);
        }
    }

    [TestMethod]
    public async Task GetSelectedAgvGraphsAsync_UndefinedGraph_ReturnsSelectedDefinedAgvGraphs()
    {
        var fixture = new EfAgvGraphRepositoryTestFixture();
        var agvGraphs = new List<AgvGraph>();

        for (int i = 0; i < 10; i++)
        {
            var agvGraph = AgvGraphGenerator.CreateDefaultGraph(Guid.NewGuid());
            agvGraphs.Add(agvGraph);
            await fixture.Repository.UpdateGraphAsync(new Graph { Nodes = agvGraph.Nodes, Edges = agvGraph.Edges });
            await fixture.Repository.AddOrUpdateAgvGraphAsync(agvGraph);
        }
        List<Guid> requestedGraphs = agvGraphs.Take(agvGraphs.Count - 1).Select(a => a.CorrelationId).ToList();
        var undefinedGraph = "UNDEFINED".ToGuid();
        requestedGraphs.Add(undefinedGraph);
        ICollection<AgvGraph> agvGraphsInRepository = await fixture.Repository.GetSelectedAgvGraphsAsync(requestedGraphs);

        agvGraphsInRepository.Count.ShouldBe(requestedGraphs.Except(new List<Guid> { undefinedGraph }).ToList().Count);
        foreach (var agvGraphInRepository in agvGraphsInRepository)
        {
            var expectedAgvGraph = agvGraphs.First(x => x.CorrelationId == agvGraphInRepository.CorrelationId);
            VerifyAgvGraph(agvGraphInRepository, expectedAgvGraph);
        }
    }
}