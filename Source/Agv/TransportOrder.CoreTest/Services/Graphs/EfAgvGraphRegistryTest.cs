using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Graphs
{
    [TestClass]
    public class EfAgvGraphRegistryTest
    {
        #region Setup
        private class EfAgvGraphRegistryTestFixture
        {
            public EfAgvGraphRegistry Registry { get; }

            public EfAgvGraphRegistryTestFixture()
            {
                var mapper = MappingHelper.GetMapper(new GraphProfile());
                var collection = new ServiceCollection()
                    .AddTransient<IMapper>(_ => mapper)
                    .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); }
                    );
                var provider = collection.BuildServiceProvider(true);
                var contextFactory = provider.GetService<IDbContextFactory<TransportOrderDbContext>>()!;
                Registry = new EfAgvGraphRegistry(contextFactory, mapper);
            }
        }

        private static void VerifyAgvGraph(AgvGraph actualAgvGraph, AgvGraph expectedAgvGraph)
        {
            actualAgvGraph.ShouldNotBeNull();
            actualAgvGraph.ShouldSatisfyAllConditions(
                e => e.CorrelationId.ShouldBe(expectedAgvGraph.CorrelationId),
                e => e.RelatedMapId.ShouldBe(expectedAgvGraph.RelatedMapId),
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
                e => e.Metric.ShouldBe(expectedEdge.Metric));
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
        #endregion

        [TestMethod]
        public async Task GetNodesAsync_WhenAgvGraphIsExisting_ReturnsNodes()
        {
            var fixture = new EfAgvGraphRegistryTestFixture();
            var correlationId = Guid.NewGuid();
            var expectedAgvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
            await fixture.Registry.AddOrUpdateAsync(expectedAgvGraph);
            List<GraphNode> expectedNodes = expectedAgvGraph.Nodes.Where(x => x.Id.Contains('1')).ToList();

            List<GraphNode> nodes = (await fixture.Registry.GetNodesAsync(correlationId, node => node.Id.Contains('1'))).ToList();

            nodes.Count.ShouldBe(expectedNodes.Count);
            foreach (var node in nodes)
            {
                VerifyGraphNode(node, expectedNodes.First(x => x.Id == node.Id));
            }
        }

        [TestMethod]
        public async Task GetNodeAsync_WhenNodeExist_ReturnsNode()
        {
            var fixture = new EfAgvGraphRegistryTestFixture();
            var correlationId = Guid.NewGuid();
            var expectedAgvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
            await fixture.Registry.AddOrUpdateAsync(expectedAgvGraph);
            var expectedNode = expectedAgvGraph.Nodes.First();

            var actualNode = await fixture.Registry.GetNodeAsync(expectedNode.Id, correlationId);

            VerifyGraphNode(actualNode, expectedNode);
        }

        [TestMethod]
        public async Task GetNodeAsync_WhenNodeIsNotFound_ThrowsNodeNotFoundException()
        {
            var fixture = new EfAgvGraphRegistryTestFixture();
            var correlationId = Guid.NewGuid();
            var expectedAgvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
            await fixture.Registry.AddOrUpdateAsync(expectedAgvGraph);

            await Should.ThrowAsync<NodeNotFoundException>(async () => await fixture.Registry.GetNodeAsync("Not existing Node Id", correlationId));
        }

        [TestMethod]
        public async Task GetAgvGraphAsync_WhenGraphIsNotExisting_ThrowsAgvGraphNotDefinedException()
        {
            var fixture = new EfAgvGraphRegistryTestFixture();

            await Should.ThrowAsync<AgvGraphNotDefinedException>(async () => await fixture.Registry.GetAgvGraphAsync(Guid.NewGuid()));
        }

        [TestMethod]
        public async Task GetAgvGraphAsync_WhenGraphIsExisting_ReturnsAgvGraph()
        {
            var fixture = new EfAgvGraphRegistryTestFixture();
            var correlationId = Guid.NewGuid();
            var expectedAgvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
            await fixture.Registry.AddOrUpdateAsync(expectedAgvGraph);

            var agvGraphInRegistry = await fixture.Registry.GetAgvGraphAsync(correlationId);

            VerifyAgvGraph(agvGraphInRegistry, expectedAgvGraph);
        }

        [TestMethod]
        public async Task GetAllAgvGraphsAsync_WhenAgvsExist_ReturnsAllAgvGraphs()
        {
            var fixture = new EfAgvGraphRegistryTestFixture();
            var agvGraphs = new List<AgvGraph>();

            for (int i = 0; i < 10; i++)
            {
                var agvGraph = AgvGraphGenerator.CreateDefaultGraph(Guid.NewGuid());
                agvGraphs.Add(agvGraph);
                await fixture.Registry.AddOrUpdateAsync(agvGraph);
            }

            List<AgvGraph> agvGraphsInRepository = await fixture.Registry.GetAllAgvGraphsAsync();

            agvGraphsInRepository.Count.ShouldBe(agvGraphs.Count);
            foreach (var agvGraphInRepository in agvGraphsInRepository)
            {
                var expectedAgvGraph = agvGraphs.First(x => x.CorrelationId == agvGraphInRepository.CorrelationId);
                VerifyAgvGraph(agvGraphInRepository, expectedAgvGraph);
            }
        }

        [TestMethod]
        public async Task ExistsAsync_WhenEfGraphIsExisting_ReturnsTrue()
        {
            var fixture = new EfAgvGraphRegistryTestFixture();
            var correlationId = Guid.NewGuid();
            var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
            await fixture.Registry.AddOrUpdateAsync(agvGraph);

            bool isExisting = await fixture.Registry.ExistsAsync(correlationId);

            isExisting.ShouldBeTrue();
        }

        [TestMethod]
        public async Task ExistsAsync_WHenEfGraphIsNotExisting_ReturnsFalse()
        {
            var fixture = new EfAgvGraphRegistryTestFixture();

            bool isExisting = await fixture.Registry.ExistsAsync(Guid.NewGuid());

            isExisting.ShouldBeFalse();
        }

        [TestMethod]
        public async Task RemoveAsync_WhenAgvGraphIsNotExisting_ThrowsAgvGraphNotDefinedException()
        {
            var fixture = new EfAgvGraphRegistryTestFixture();

            await Should.ThrowAsync<AgvGraphNotDefinedException>(async () => await fixture.Registry.RemoveAsync(Guid.NewGuid()));
        }

        [TestMethod]
        public async Task RemoveAsync_WhenAgvGraphIsExisting_DeletesSuccessfully()
        {
            var fixture = new EfAgvGraphRegistryTestFixture();
            var correlationId = Guid.NewGuid();
            var expectedAgvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
            await fixture.Registry.AddOrUpdateAsync(expectedAgvGraph);
            var agvGraphInRepository = await fixture.Registry.GetAgvGraphAsync(correlationId);
            agvGraphInRepository.ShouldNotBeNull();
            VerifyAgvGraph(agvGraphInRepository, expectedAgvGraph);

            await fixture.Registry.RemoveAsync(correlationId);

            await Should.ThrowAsync<AgvGraphNotDefinedException>(async () => await fixture.Registry.GetAgvGraphAsync(correlationId));
        }
    }
}