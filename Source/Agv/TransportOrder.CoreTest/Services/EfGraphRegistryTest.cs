using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using NodeState = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvGraph.NodeState;
using NodeType = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvGraph.NodeType;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
{
    [TestClass]
    public class EfGraphRegistryTest
    {
        #region Setup
        private class EfGraphRegistryTestFixture
        {
            private ServiceProvider Provider { get; }
            private IDbContextFactory<SagaPersistenceDbContext> ContextFactory { get; }
            public EfGraphRegistry Registry { get; }

            public EfGraphRegistryTestFixture()
            {
                var mockLogger = new Mock<ILogger<EfGraphRegistry>>();
                var settingsProviderMock = new Mock<IAgvSettingsProvider>();
                settingsProviderMock.Setup(x => x.GetSettingsForAgv(It.IsAny<Guid>())).Returns(new AgvSettings());

                var collection = new ServiceCollection()
                    .AddDbContextFactory<SagaPersistenceDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                    }
                );
                Provider = collection.BuildServiceProvider(true);
                ContextFactory = Provider.GetService<IDbContextFactory<SagaPersistenceDbContext>>()!;
                Registry = new EfGraphRegistry(ContextFactory, mockLogger.Object,  settingsProviderMock.Object);
            }
        }

        private static Graph GetTestGraph()
        {
            // Create test graph
            var nodes = new Node[]
            {
                new() { Id = "NODE_01", State = NodeState.Free,     Type = NodeType.Charge, X = 10.0f, Y = 10.0f  }, // 1 ---- 2
                new() { Id = "NODE_02", State = NodeState.Reserved, Type = NodeType.Charge, X = 20.0f, Y = 10.0f  }, // |      |
                new() { Id = "NODE_03", State = NodeState.Occupied, Type = NodeType.Charge, X = 20.0f, Y = 20.0f  }, // |      |
                new() { Id = "NODE_04", State = NodeState.Locked,   Type = NodeType.Charge, X = 10.0f, Y = 20.0f  }  // 4 ---- 3
            };

            var edges = new Edge[]
            {
                new() { FromId = "NODE_01", ToId = "NODE_02", Metric = 1.0f },  // 1 -> 2
                new() { FromId = "NODE_02", ToId = "NODE_01", Metric = 1.0f },  // 2 -> 1

                new() { FromId = "NODE_02", ToId = "NODE_03", Metric = 1.0f },  // 2 -> 3
                new() { FromId = "NODE_03", ToId = "NODE_02", Metric = 1.0f },  // 3 -> 2

                new() { FromId = "NODE_03", ToId = "NODE_04", Metric = 1.0f },  // 3 -> 4
                new() { FromId = "NODE_04", ToId = "NODE_03", Metric = 1.0f },  // 4 -> 3

                new() { FromId = "NODE_04", ToId = "NODE_01", Metric = 1.0f },  // 4 -> 1
                new() { FromId = "NODE_01", ToId = "NODE_04", Metric = 1.0f }   // 1 -> 4
            };

            return new Graph(nodes, edges);
        }
        #endregion

        [TestMethod]
        public void Test_GetGraph()
        {
            var fixture = new EfGraphRegistryTestFixture();

            var testGraph = GetTestGraph();
            int nodesCount = testGraph.Nodes.Length;
            int edgesCount = testGraph.Edges.Length;

            fixture.Registry.AddOrUpdateGraph(testGraph).ShouldBeTrue();

            var graph = fixture.Registry.GetGraph();
            graph.Nodes.Length.ShouldBe(nodesCount);
            graph.Edges.Length.ShouldBe(edgesCount);

            for (int i = 0; i < nodesCount; i++)
            {
                graph.Nodes[i].Id.ShouldBe(testGraph.Nodes[i].Id);
                graph.Nodes[i].X.ShouldBe(testGraph.Nodes[i].X);
                graph.Nodes[i].Y.ShouldBe(testGraph.Nodes[i].Y);
                graph.Nodes[i].State.ShouldBe(testGraph.Nodes[i].State);
            }

            for (int i = 0; i < edgesCount; i++)
            {
                graph.Edges[i].FromId.ShouldBe(testGraph.Edges[i].FromId);
                graph.Edges[i].ToId.ShouldBe(testGraph.Edges[i].ToId);
                graph.Edges[i].Metric.ShouldBe(testGraph.Edges[i].Metric);
            }
        }

        [TestMethod]
        public void Test_GetNodes()
        {
            var fixture = new EfGraphRegistryTestFixture();

            var testGraph = GetTestGraph();
            fixture.Registry.AddOrUpdateGraph(testGraph).ShouldBeTrue();

            // Get all nodes
            List<Node> nodes = fixture.Registry.GetNodes().ToList();
            nodes.Count.ShouldBe(testGraph.Nodes.Length);

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Id.ShouldBe(testGraph.Nodes[i].Id);
                nodes[i].X.ShouldBe(testGraph.Nodes[i].X);
                nodes[i].Y.ShouldBe(testGraph.Nodes[i].Y);
                nodes[i].State.ShouldBe(testGraph.Nodes[i].State);
            }

            // Get specific node
            nodes = fixture.Registry.GetNodes(node => node.Id == "NODE_01").ToList();
            nodes.Count.ShouldBe(1);
            nodes[0].Id.ShouldBe("NODE_01");

            // Get all reserved nodes
            nodes = fixture.Registry.GetNodes(node => node.State == NodeState.Reserved).ToList();
            nodes.Count.ShouldBe(1);
            nodes[0].Id.ShouldBe("NODE_02");
        }

        [TestMethod]
        public void Test_GetEdges()
        {
            var fixture = new EfGraphRegistryTestFixture();

            var testGraph = GetTestGraph();
            fixture.Registry.AddOrUpdateGraph(testGraph).ShouldBeTrue();

            // Get all edges
            List<Edge> edges = fixture.Registry.GetEdges().ToList();
            edges.Count.ShouldBe(testGraph.Edges.Length);

            for (int i = 0; i < edges.Count; i++)
            {
                edges[i].FromId.ShouldBe(testGraph.Edges[i].FromId);
                edges[i].ToId.ShouldBe(testGraph.Edges[i].ToId);
                edges[i].Metric.ShouldBe(testGraph.Edges[i].Metric);
            }

            // Get specific edge
            edges = fixture.Registry.GetEdges(edge => edge.FromId == "NODE_01" && edge.ToId == "NODE_02").ToList();
            edges.Count.ShouldBe(1);
            edges[0].FromId.ShouldBe("NODE_01");
            edges[0].ToId.ShouldBe("NODE_02");
        }

        [TestMethod]
        public void Test_AddOrUpdateNode()
        {
            var fixture = new EfGraphRegistryTestFixture();

            var graph = fixture.Registry.GetGraph();
            graph.Nodes.Length.ShouldBe(0); // Graph still empty

            // Add first node
            var node = new Node() { Id = "NODE_05", X = 10.0f, Y = 10.0f, State = NodeState.Free };
            int nodesCount = 1;
            fixture.Registry.AddOrUpdateNode(node).ShouldBeTrue();
            graph = fixture.Registry.GetGraph();
            graph.Nodes.Length.ShouldBe(nodesCount);
            node = graph.Nodes[nodesCount - 1];
            node.Id.ShouldBe("NODE_05");
            node.State.ShouldBe(NodeState.Free);

            // Add another node
            node = new Node() { Id = "NODE_06", X = 10.0f, Y = 10.0f, State = NodeState.Reserved };
            nodesCount++; // Node count increase
            fixture.Registry.AddOrUpdateNode(node).ShouldBeTrue();
            graph = fixture.Registry.GetGraph();
            graph.Nodes.Length.ShouldBe(nodesCount);
            node = graph.Nodes[nodesCount - 1];
            node.Id.ShouldBe("NODE_06");
            node.State.ShouldBe(NodeState.Reserved);

            // Update
            node.State = NodeState.Occupied;
            fixture.Registry.AddOrUpdateNode(node).ShouldBeTrue();
            graph = fixture.Registry.GetGraph();
            graph.Nodes.Length.ShouldBe(nodesCount);
            graph.Nodes[nodesCount - 1].State.ShouldBe(node.State);
        }

        [TestMethod]
        public void Test_AddOrUpdateEdge()
        {
            var fixture = new EfGraphRegistryTestFixture();

            var graph = fixture.Registry.GetGraph();
            graph.Edges.Length.ShouldBe(0); // Graph still empty

            // Add first edge
            var edge = new Edge() { FromId = "NODE_05", ToId = "NODE_01", Metric = 1.0f };
            int edgesCount = 1;
            fixture.Registry.AddOrUpdateEdge(edge).ShouldBeTrue();
            graph = fixture.Registry.GetGraph();
            graph.Edges.Length.ShouldBe(edgesCount);
            edge = graph.Edges[edgesCount - 1];
            edge.FromId.ShouldBe("NODE_05");
            edge.ToId.ShouldBe("NODE_01");
            edge.Metric.ShouldBe(1.0f);

            // Add another edge
            edge = new Edge() { FromId = "NODE_01", ToId = "NODE_05", Metric = 1.5f };
            edgesCount++; // Edge count increase
            fixture.Registry.AddOrUpdateEdge(edge).ShouldBeTrue();
            graph = fixture.Registry.GetGraph();
            graph.Edges.Length.ShouldBe(edgesCount);
            edge = graph.Edges[edgesCount - 1];
            edge.FromId.ShouldBe("NODE_01");
            edge.ToId.ShouldBe("NODE_05");
            edge.Metric.ShouldBe(1.5f);

            // Update existing edge
            edge.Metric = 2.0f;
            fixture.Registry.AddOrUpdateEdge(edge).ShouldBeTrue();
            graph = fixture.Registry.GetGraph();
            graph.Edges.Length.ShouldBe(edgesCount);
            edge = graph.Edges[edgesCount - 1];
            edge.FromId.ShouldBe("NODE_01");
            edge.ToId.ShouldBe("NODE_05");
            edge.Metric.ShouldBe(2.0f);
        }

        [TestMethod]
        public void Test_AddOrUpdateGraph()
        {
            var fixture = new EfGraphRegistryTestFixture();

            // Add initial graph
            var testGraph = GetTestGraph();
            int nodesCount = testGraph.Nodes.Length;
            int edgesCount = testGraph.Edges.Length;

            fixture.Registry.AddOrUpdateGraph(testGraph).ShouldBeTrue();

            var graph = fixture.Registry.GetGraph();
            graph.Nodes.Length.ShouldBe(nodesCount);
            graph.Edges.Length.ShouldBe(edgesCount);

            // Add new nodes / edges -> count should increase
            var newNodes = new Node[]
            {
                new() { Id = "NODE_05", X = 15.0f, Y = 15.0f, State = NodeState.Free }
            };

            var newEdges = new Edge[]
            {
                new() { FromId = "NODE_05", ToId = "NODE_01", Metric = 1.0f },
                new() { FromId = "NODE_01", ToId = "NODE_05", Metric = 1.0f }
            };

            testGraph = new(newNodes, newEdges);
            nodesCount += testGraph.Nodes.Length; // Edges are aggregated so count increases
            edgesCount += testGraph.Edges.Length; // Nodes are aggregated so count increases

            fixture.Registry.AddOrUpdateGraph(testGraph).ShouldBeTrue();

            graph = fixture.Registry.GetGraph();
            graph.Nodes.Length.ShouldBe(nodesCount);
            graph.Edges.Length.ShouldBe(edgesCount);

            // Add same nodes / edges again -> count should stay the same
            fixture.Registry.AddOrUpdateGraph(testGraph).ShouldBeTrue();

            graph = fixture.Registry.GetGraph();
            graph.Nodes.Length.ShouldBe(nodesCount);
            graph.Edges.Length.ShouldBe(edgesCount);
        }

        [TestMethod]
        [DataRow("NODE_01", true, NodeState.Locked, NodeState.Reserved, NodeState.Occupied, NodeState.Locked)]
        [DataRow("NODE_02", true, NodeState.Free, NodeState.Locked, NodeState.Occupied, NodeState.Locked)]
        [DataRow("NODE_03", true, NodeState.Free, NodeState.Reserved, NodeState.Locked, NodeState.Locked)]
        [DataRow("NODE_04", true, NodeState.Free, NodeState.Reserved, NodeState.Occupied, NodeState.Locked)]
        [DataRow("NODE_XX", false, NodeState.Free, NodeState.Reserved, NodeState.Occupied, NodeState.Locked)] // Unknown node -> no change
        public void Test_LockNode(string nodeId, bool hasChanged, NodeState state1, NodeState state2, NodeState state3, NodeState state4)
        {
            var fixture = new EfGraphRegistryTestFixture();

            // Add initial graph
            var testGraph = GetTestGraph();
            fixture.Registry.AddOrUpdateGraph(testGraph).ShouldBeTrue();

            var graph = fixture.Registry.GetGraph();
            graph.Nodes.Length.ShouldBe(4);
            graph.Nodes[0].State.ShouldBe(NodeState.Free);
            graph.Nodes[1].State.ShouldBe(NodeState.Reserved);
            graph.Nodes[2].State.ShouldBe(NodeState.Occupied);
            graph.Nodes[3].State.ShouldBe(NodeState.Locked);

            // Try to lock node
            fixture.Registry.LockNode(nodeId).ShouldBe(hasChanged);
            graph = fixture.Registry.GetGraph();
            graph.Nodes[0].State.ShouldBe(state1);
            graph.Nodes[1].State.ShouldBe(state2);
            graph.Nodes[2].State.ShouldBe(state3);
            graph.Nodes[3].State.ShouldBe(state4);
        }

        [TestMethod]
        [DataRow("NODE_01", true, NodeState.Free, NodeState.Reserved, NodeState.Occupied, NodeState.Locked)]
        [DataRow("NODE_02", true, NodeState.Free, NodeState.Free, NodeState.Occupied, NodeState.Locked)]
        [DataRow("NODE_03", true, NodeState.Free, NodeState.Reserved, NodeState.Free, NodeState.Locked)]
        [DataRow("NODE_04", true, NodeState.Free, NodeState.Reserved, NodeState.Occupied, NodeState.Free)]
        [DataRow("NODE_XX", false, NodeState.Free, NodeState.Reserved, NodeState.Occupied, NodeState.Locked)] // Unknown node -> no change
        public void Test_FreeNode(string nodeId, bool hasChanged, NodeState state1, NodeState state2, NodeState state3, NodeState state4)
        {
            var fixture = new EfGraphRegistryTestFixture();

            // Add initial graph
            var testGraph = GetTestGraph();
            fixture.Registry.AddOrUpdateGraph(testGraph).ShouldBeTrue();

            var graph = fixture.Registry.GetGraph();
            graph.Nodes.Length.ShouldBe(4);
            graph.Nodes[0].State.ShouldBe(NodeState.Free);
            graph.Nodes[1].State.ShouldBe(NodeState.Reserved);
            graph.Nodes[2].State.ShouldBe(NodeState.Occupied);
            graph.Nodes[3].State.ShouldBe(NodeState.Locked);

            // Try to free node
            fixture.Registry.FreeNode(nodeId).ShouldBe(hasChanged);
            graph = fixture.Registry.GetGraph();
            graph.Nodes[0].State.ShouldBe(state1);
            graph.Nodes[1].State.ShouldBe(state2);
            graph.Nodes[2].State.ShouldBe(state3);
            graph.Nodes[3].State.ShouldBe(state4);
        }

        [TestMethod]
        [DataRow("NODE_01", true, NodeState.Reserved, NodeState.Reserved, NodeState.Occupied, NodeState.Locked)] // Onyl NODE_01 should change
        [DataRow("NODE_02", false, NodeState.Free, NodeState.Reserved, NodeState.Occupied, NodeState.Locked)]
        [DataRow("NODE_03", false, NodeState.Free, NodeState.Reserved, NodeState.Occupied, NodeState.Locked)]
        [DataRow("NODE_04", false, NodeState.Free, NodeState.Reserved, NodeState.Occupied, NodeState.Locked)]
        [DataRow("NODE_XX", false, NodeState.Free, NodeState.Reserved, NodeState.Occupied, NodeState.Locked)] // Unknown node -> no change
        public void Test_ReserveNode(string nodeId, bool hasChanged, NodeState state1, NodeState state2, NodeState state3, NodeState state4)
        {
            var fixture = new EfGraphRegistryTestFixture();

            // Add initial graph
            var testGraph = GetTestGraph();
            fixture.Registry.AddOrUpdateGraph(testGraph).ShouldBeTrue();

            var graph = fixture.Registry.GetGraph();
            graph.Nodes.Length.ShouldBe(4);
            graph.Nodes[0].State.ShouldBe(NodeState.Free);
            graph.Nodes[1].State.ShouldBe(NodeState.Reserved);
            graph.Nodes[2].State.ShouldBe(NodeState.Occupied);
            graph.Nodes[3].State.ShouldBe(NodeState.Locked);

            // Try to reserve node
            fixture.Registry.ReserveNode(nodeId).ShouldBe(hasChanged);
            graph = fixture.Registry.GetGraph();
            graph.Nodes[0].State.ShouldBe(state1);
            graph.Nodes[1].State.ShouldBe(state2);
            graph.Nodes[2].State.ShouldBe(state3);
            graph.Nodes[3].State.ShouldBe(state4);
        }

        [TestMethod]
        [DataRow("NODE_01", 10.0f, 10.0f, NodeState.Occupied, NodeState.Reserved, NodeState.Free, NodeState.Locked)]
        [DataRow("NODE_02", 20.0f, 10.0f, NodeState.Free, NodeState.Occupied, NodeState.Free, NodeState.Locked)]
        [DataRow("NODE_03", 20.0f, 20.0f, NodeState.Free, NodeState.Reserved, NodeState.Occupied, NodeState.Locked)]
        [DataRow("NODE_04", 10.0f, 20.0f, NodeState.Free, NodeState.Reserved, NodeState.Free, NodeState.Locked)]
        [DataRow(null, 30.0f, 30.0f, NodeState.Free, NodeState.Reserved, NodeState.Free, NodeState.Locked)]
        public void Test_UpdateOccupiedNode(string? currNodeId, float posX, float posY, NodeState state1, NodeState state2, NodeState state3, NodeState state4)
        {
            var fixture = new EfGraphRegistryTestFixture();

            // Add initial graph
            var testGraph = GetTestGraph();
            fixture.Registry.AddOrUpdateGraph(testGraph).ShouldBeTrue();

            string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            // Setup initial position of AGV -> NODE_3
            var agvPositionStatus = new AgvPositionStatus(correlationId, agvId, new AgvPosition(true, 20.0f, 20.0f, 0, 0));
            fixture.Registry.UpdateOccupiedNode(agvPositionStatus, out var prevNode, out var currNode).ShouldBeTrue();
            currNode.ShouldNotBeNull();
            currNode.Id.ShouldBe("NODE_03");

            // AGV moves
            agvPositionStatus = new AgvPositionStatus(correlationId, agvId, new AgvPosition(true, posX, posY, 0, 0));
            fixture.Registry.UpdateOccupiedNode(agvPositionStatus, out prevNode, out currNode).ShouldBeTrue();
            prevNode.ShouldNotBeNull();
            prevNode.Id.ShouldBe("NODE_03");

            if (currNodeId == null)
            {
                currNode.ShouldBeNull();
            }
            else
            {
                currNode.ShouldNotBeNull();
                currNode.Id.ShouldBe(currNodeId);
            }

            var graph = fixture.Registry.GetGraph();
            graph.Nodes[0].State.ShouldBe(state1);
            graph.Nodes[1].State.ShouldBe(state2);
            graph.Nodes[2].State.ShouldBe(state3);
            graph.Nodes[3].State.ShouldBe(state4);
        }

        [TestMethod]
        public void Test_SetAllOccupiedNodes()
        {
            var fixture = new EfGraphRegistryTestFixture();
            var graph = GetTestGraph();

            bool result = fixture.Registry.AddOrUpdateGraph(graph);
            result.ShouldBeTrue();

            graph = fixture.Registry.GetGraph();
            graph.Nodes.Length.ShouldBe(4);

            graph.Nodes[0].State.ShouldBe(NodeState.Free);
            graph.Nodes[1].State.ShouldBe(NodeState.Reserved);
            graph.Nodes[2].State.ShouldBe(NodeState.Occupied);
            graph.Nodes[3].State.ShouldBe(NodeState.Locked);

            // Create test positions
            var agvPositions = new List<AgvPositionStatus>
            {
               new(Guid.Empty, "", new AgvPosition(true, 10.0f, 10.0f, 0, 0)), // -> NODE_01
               new(Guid.Empty, "", new AgvPosition(true, 30.0f, 10.0f, 0, 0)) // -> No node
            };

            fixture.Registry.SetAllOccupiedNodes(agvPositions).ShouldBeTrue();
            graph = fixture.Registry.GetGraph();

            graph.Nodes[0].State.ShouldBe(NodeState.Occupied);  // Now Occupied
            graph.Nodes[1].State.ShouldBe(NodeState.Reserved);
            graph.Nodes[2].State.ShouldBe(NodeState.Free);      // Now Free
            graph.Nodes[3].State.ShouldBe(NodeState.Locked);

            // Add position to occupy NODE_02
            agvPositions.Add(new AgvPositionStatus(Guid.Empty, "", new AgvPosition(true, 20.0f, 10.0f, 0, 0))); // -> NODE_02

            fixture.Registry.SetAllOccupiedNodes(agvPositions).ShouldBeTrue();
            graph = fixture.Registry.GetGraph();

            graph.Nodes[0].State.ShouldBe(NodeState.Occupied);
            graph.Nodes[1].State.ShouldBe(NodeState.Occupied);  // Now Occupied
            graph.Nodes[2].State.ShouldBe(NodeState.Free);
            graph.Nodes[3].State.ShouldBe(NodeState.Locked);

            // Add one more posistion to occupy NODE_04 -> should have no effect fail because NODE_04 is locked
            agvPositions.Add(new AgvPositionStatus(Guid.Empty, "", new AgvPosition(true, 10.0f, 20.0f, 0, 0))); // -> NODE_04

            fixture.Registry.SetAllOccupiedNodes(agvPositions).ShouldBeTrue();
            graph = fixture.Registry.GetGraph();

            graph.Nodes[0].State.ShouldBe(NodeState.Occupied);
            graph.Nodes[1].State.ShouldBe(NodeState.Occupied);
            graph.Nodes[2].State.ShouldBe(NodeState.Free);
            graph.Nodes[3].State.ShouldBe(NodeState.Locked);    // Still locked

            // Free NODE_01
            agvPositions[0] = new AgvPositionStatus(Guid.Empty, "", new AgvPosition(true, 10.0f, 30.0f, 0, 0)); // -> No node

            fixture.Registry.SetAllOccupiedNodes(agvPositions).ShouldBeTrue();
            graph = fixture.Registry.GetGraph();

            graph.Nodes[0].State.ShouldBe(NodeState.Free); // Now Free
            graph.Nodes[1].State.ShouldBe(NodeState.Occupied);
            graph.Nodes[2].State.ShouldBe(NodeState.Free);
            graph.Nodes[3].State.ShouldBe(NodeState.Locked);
        }
    }
}
