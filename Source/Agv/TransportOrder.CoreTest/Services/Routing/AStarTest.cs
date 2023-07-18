using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Routing
{
    /// <summary>
    /// Test for utility <see cref="AStar"/>.
    /// </summary>
    [TestClass]
    public class AStarTest
    {
        private readonly AgvGraph? _graph;
        private readonly AgvGraph? _grid;
        private readonly AgvGraph _wikiExample;
        private readonly Mock<IRoutingPlausibilityChecker> _routingPlausibilityCheckerMock;

        private IRoutingPlausibilityChecker RoutingPlausibilityChecker => _routingPlausibilityCheckerMock.Object;

        public AStarTest()
        {
            const string mapName = "Map#1";
            _graph = new AgvGraph(Guid.Empty,
                new GraphNode[]
                {
                    new("1") { Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 5f, Z = 0f },
                    new("2") { Type = AgvGraph.NodeType.PickDrop, X = 1f, Y = 5f, Z = 0f },
                    new("3") { Type = AgvGraph.NodeType.PickDrop, X = 1f, Y = 2f, Z = 0f },
                    new("4") { Type = AgvGraph.NodeType.PickDrop, X = 3f, Y = 3f, Z = 0f },
                    new("5") { Type = AgvGraph.NodeType.PickDrop, X = 3f, Y = 6f, Z = 0f },
                    new("6") { Type = AgvGraph.NodeType.PickDrop, X = 3f, Y = 2f, Z = 0f },
                    new("7") { Type = AgvGraph.NodeType.PickDrop, X = 6f, Y = 0f, Z = 0f },
                    new("8") { Type = AgvGraph.NodeType.PickDrop, X = 7f, Y = 1f, Z = 0f },
                    new("9") { Type = AgvGraph.NodeType.PickDrop, X = 8f, Y = 2f, Z = 0f },
                    new("10") { Type = AgvGraph.NodeType.PickDrop, X = 8f, Y = 5f, Z = 0f }
                },
                new GraphEdge[]
                {
                    new("1", "2", 1f),
                    new("1", "4", 4f),
                    new("4", "1", 4f),
                    new("2", "5", 3f),
                    new("3", "5", 5f),
                    new("3", "4", 3f),
                    new("4", "3", 3f),
                    new("3", "6", 2f),
                    new("6", "3", 2f),
                    new("5", "8", 7f),
                    new("8", "5", 7f),
                    new("8", "7", 1f),
                    new("7", "8", 1f),
                    new("9", "8", 1f),
                    new("7", "10", 6f)
                },
                mapName.ToGuid(),
                mapName);

            var map = new List<string>
            {
                // --> x [1..50]
                // 12345678901234567890123456789012345678901234567890
                "           xxxxxx    xxx    xxx   xx  xx  x x x x ", // 01 |
                "        x                                         ", // 02 |
                "        x                                         ", // 03 v
                "    xxxxx                                         ", // 04
                "        x  ooooooooooooooooooooooo                ", // 05 y [1..25}
                "        x  o   o   o   o   o   o o                ", // 06
                "           o o o o o o o o o o o o                ", // 07
                "           o o o o o o o o o o o o                ", // 08
                "           o o o o o o o o o o o o                ", // 09
                "           o o o o o o o o o o o o                ", // 10
                "           o o o o o o o o o o o o                ", // 11
                "           o o o o o o o o o o o o                ", // 12
                "           o o o o o o o o o o o o                ", // 13
                "           o o o o o o o o o o o o                ", // 14
                "           o o o o o o o o o o o o                ", // 15
                "           o o o o o o o o o o o o                ", // 16
                "           o o   o   o   o   o   o                ", // 17
                "           o ooooooooooooooooooooo                ", // 18
                "                                                  ", // 19
                "                                                  ", // 20
                "                                                  ", // 21
                "                                            xxxxx ", // 22
                "                                            x   x ", // 23
                "                                            xxxxx ", // 24
                "                                                  " // 25
            }; // 1030 nodes / 3548 edges
            var nodes = new List<GraphNode>();
            var edges = new List<GraphEdge>();
            int x, y = 1;
            foreach (string line in map)
            {
                x = 1;
                foreach (char c in line)
                {
                    if (c == ' ')
                    {
                        var next = new GraphNode($"{x:D2}/{y:D2}") { X = x, Y = y, Z = 0f, Type = AgvGraph.NodeType.WayPoint };
                        nodes.Add(next);
                        var prev = nodes.SingleOrDefault(n => n.Id == $"{x - 1:D2}/{y:D2}");
                        if (prev != null)
                        {
                            edges.Add(new GraphEdge(prev.Id, next.Id, 1f));
                            edges.Add(new GraphEdge(next.Id, prev.Id, 1f));
                        }
                        prev = nodes.SingleOrDefault(n => n.Id == $"{x:D2}/{y - 1:D2}");
                        if (prev != null)
                        {
                            edges.Add(new GraphEdge(prev.Id, next.Id, 1f) { Metric = 1f });
                            edges.Add(new GraphEdge(next.Id, prev.Id, 1f) { Metric = 1f });
                        }
                    }
                    ++x;
                }
                ++y;
            }
            _grid = new AgvGraph(Guid.Empty, nodes.ToArray(), edges.ToArray(), mapName.ToGuid(), mapName);

            _wikiExample = new AgvGraph(Guid.Empty,
                new GraphNode[]
                {
                    new("Saarbrücken") { Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f },
                    new("Kaiserslautern") { Type = AgvGraph.NodeType.PickDrop, X = 42f, Y = 80f, Z = 0f },
                    new("Frankfurt") { Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = 100f, Z = 0f },
                    new("Ludwigshafen") { Type = AgvGraph.NodeType.PickDrop, X = 96f, Y = 60f, Z = 0f },
                    new("Karlsruhe") { Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = -10f, Z = 0f },
                    new("Heilbronn") { Type = AgvGraph.NodeType.PickDrop, X = 150f, Y = 20f, Z = 0f },
                    new("Würzburg") { Type = AgvGraph.NodeType.PickDrop, X = 200f, Y = 92f, Z = 0f }
                },
                new GraphEdge[]
                {
                    new("Saarbrücken", "Kaiserslautern", 70f),
                    new("Saarbrücken", "Karlsruhe", 145f),
                    new("Karlsruhe", "Saarbrücken", 145f),
                    new("Kaiserslautern", "Frankfurt", 103f),
                    new("Kaiserslautern", "Ludwigshafen", 53f),
                    new("Karlsruhe", "Heilbronn", 84f),
                    new("Frankfurt", "Würzburg", 116f),
                    new("Ludwigshafen", "Würzburg", 183f),
                    new("Heilbronn", "Würzburg", 102f)
                },
                mapName.ToGuid(),
                mapName);

            _routingPlausibilityCheckerMock = new Mock<IRoutingPlausibilityChecker>();
            _routingPlausibilityCheckerMock.Setup(c => c.IsPlausible(It.IsAny<GraphNode>(), It.IsAny<GraphNode>()))
                .Returns(true);
        }

        /// <summary>
        /// Test sorting of <see cref="Common.Definitions.IGraphAlgorithm.Route"/>.
        /// </summary>
        [TestMethod]
        public void Test_RouteComparable()
        {
            var routes = new List<IGraphAlgorithm.Route?>
            {
                new(9f, Array.Empty<string>()),
                new(1f, Array.Empty<string>()),
                null,
                new(5f, Array.Empty<string>())
            };

            List<IGraphAlgorithm.Route?> sorted = routes.OrderByDescending(n => n != null).ThenBy(n => n).ToList();
            sorted[0]!.Costs.ShouldBe(routes[1]!.Costs, 0.00001f);
            sorted[1]!.Costs.ShouldBe(routes[3]!.Costs, 0.00001f);
            sorted[2]!.Costs.ShouldBe(routes[0]!.Costs, 0.00001f);
            sorted[3].ShouldBeNull();
        }

        /// <summary>
        /// Test AStar implementation with example from Wikipedia.
        /// </summary>
        [TestMethod]
        public void Test_AStar_WikipediaExample()
        {
            var aStar = new AStar(_wikiExample, RoutingPlausibilityChecker);

            var route = aStar.CalculateRoute("Saarbrücken", "Würzburg");
            route.ShouldNotBeNull();
            route.Nodes.ShouldBe(new[] { "Saarbrücken", "Kaiserslautern", "Frankfurt", "Würzburg" });
            route.Costs.ShouldBe(289f, 0.00001f);
        }

        [TestMethod]
        public void Test_AStar_WikipediaExample_SameSourceDestination()
        {
            var aStar = new AStar(_wikiExample, RoutingPlausibilityChecker);

            var route = aStar.CalculateRoute("Saarbrücken", "Saarbrücken");
            route.ShouldNotBeNull();
            route.Nodes.ShouldBe(new[] { "Saarbrücken" });
            route.Costs.ShouldBe(0f, 0.00001f);
        }

        [TestMethod]
        public void Test_AStar_WikipediaExample_WithCurrentNode()
        {
            var aStar = new AStar(_wikiExample, RoutingPlausibilityChecker);

            var route = aStar.CalculateRoute("Karlsruhe", "Saarbrücken", "Würzburg");
            route.ShouldNotBeNull();
            route.Nodes.ShouldBe(new[] { "Karlsruhe", "Saarbrücken", "Kaiserslautern", "Frankfurt", "Würzburg" });
            route.Costs.ShouldBe(289f + 145f, 0.00001f);
        }

        [TestMethod]
        public void Test_AStar_WikipediaExample_MultipleWaypoints()
        {
            var aStar = new AStar(_wikiExample, RoutingPlausibilityChecker);

            string[] waypoints = new[] { "Karlsruhe", "Saarbrücken", "Saarbrücken", "Würzburg" }; // (Karlsruhe => Saarbrücken) + (Saarbrücken => Würzburg)

            var route = aStar.CalculateRoute(waypoints);
            route.ShouldNotBeNull();
            route.Nodes.ShouldBe(new[] { "Karlsruhe", "Saarbrücken", "Kaiserslautern", "Frankfurt", "Würzburg" });
            route.Costs.ShouldBe(289f + 145f, 0.00001f);
        }

        /// <summary>
        /// Test AStar implementation with am example having bidirectional edges.
        /// </summary>
        [TestMethod]
        public void Test_AStarBidirectional_UseGraphSeveralTimes()
        {
            var aStar = new AStar(_graph!, RoutingPlausibilityChecker);

            var route = aStar.CalculateRoute("1", "10");
            route.ShouldNotBeNull();
            route.Costs.ShouldBe(18f, 0.00001f);

            route = aStar.CalculateRoute("10", "1");
            route.ShouldBeNull();

            route = aStar.CalculateRoute("9", "5");
            route.ShouldNotBeNull();
            route.Costs.ShouldBe(8f, 0.00001f);

            route = aStar.CalculateRoute("4", "9");
            route.ShouldBeNull();
        }

        /// <summary>
        /// Intensiv path test.
        /// </summary>
        [TestMethod]
        public void Test_AStar_GridExample()
        {
            var aStar = new AStar(_grid!, RoutingPlausibilityChecker);
            // ReSharper disable JoinDeclarationAndInitializer
            IGraphAlgorithm.Route? c1, c2, c3, c4, costs;
            // ReSharper restore JoinDeclarationAndInitializer

            c1 = aStar.CalculateRoute("01/01", "11/01");
            c1!.Costs.ShouldBe(10f, 0.00001f);
            c2 = aStar.CalculateRoute("11/01", "11/02");
            c2!.Costs.ShouldBe(1f, 0.00001f);
            c3 = aStar.CalculateRoute("11/02", "50/02");
            c3!.Costs.ShouldBe(39f, 0.00001f);
            c4 = aStar.CalculateRoute("50/02", "50/01");
            c4!.Costs.ShouldBe(1f, 0.00001f);

            costs = aStar.CalculateRoute("01/01", "50/01");
            costs!.Costs.ShouldBe(c1.Costs + c2.Costs + c3.Costs + c4.Costs, 0.00001f);

            c1 = aStar.CalculateRoute("50/01", "13/17");
            c1!.Costs.ShouldBe(57f, 0.00001f);
            c2 = aStar.CalculateRoute("13/17", "33/17");
            c2!.Costs.ShouldBe(10f * 13f, 0.00001f);
            c3 = aStar.CalculateRoute("33/17", "33/06");
            c3!.Costs.ShouldBe(11f, 0.00001f);

            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            costs = aStar.CalculateRoute("50/01", "33/06");
            stopWatch.Stop();
            System.Diagnostics.Trace.WriteLine("Grid example duration: " + stopWatch.ElapsedMilliseconds.ToString() + "ms.");
            costs!.Costs.ShouldBe(c1.Costs + c2.Costs + c3.Costs, 0.00001f);
        }

        /// <summary>
        /// Test path to or from non existent node.
        /// </summary>
        [TestMethod]
        [DataRow("01/01", "XX/XX")]
        [DataRow("YY/YY", "01/01")]
        [DataRow("YY/YY", "XX/XX")]
        public void Test_AStarGrid_NonExistentSourceOrDestination_NoPath(string source, string destination)
        {
            var aStar = new AStar(_grid!, RoutingPlausibilityChecker);
            aStar.CalculateRoute(source, destination).ShouldBeNull();
        }

        /// <summary>
        /// Test path to unreachable node.
        /// </summary>
        [TestMethod]
        public void Test_AStarGrid_UnreachableNode_NoPath()
        {
            var aStar = new AStar(_grid!, RoutingPlausibilityChecker);
            var route = aStar.CalculateRoute("01/01", "47/23");
            route.ShouldBeNull();
        }

        [TestMethod]
        public void CalculateRoute_ReturnsNull_WhenRouteNotPlausible()
        {
            const string mapName = "Map#1";
            var graph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new GraphNode("1") { Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 5f, Z = 0f },
                    new GraphNode("2") { Type = AgvGraph.NodeType.Drop, X = 1f, Y = 5f, Z = 0f }
                },
                new[]
                {
                    new GraphEdge("1", "2", 1f),
                    new GraphEdge("2", "1", 4f)
                },
                mapName.ToGuid(),
                mapName);

            var routingPlausibilityCheckerMock = new Mock<IRoutingPlausibilityChecker>();
            routingPlausibilityCheckerMock.Setup(r => r.IsPlausible(It.IsAny<GraphNode>(), It.IsAny<GraphNode>())).Returns(true);

            var aStar = new AStar(graph, routingPlausibilityCheckerMock.Object);
            var route = aStar.CalculateRoute("1", "2");
            route.ShouldNotBeNull();

            routingPlausibilityCheckerMock.Setup(r => r.IsPlausible(It.IsAny<GraphNode>(), It.IsAny<GraphNode>())).Returns(false);
            aStar = new AStar(graph, routingPlausibilityCheckerMock.Object);
            route = aStar.CalculateRoute("1", "2");
            route.ShouldBeNull();
        }

        [TestMethod]
        public void FindsShortestPossibleRoute_WhenAllPossibleRoutesBlocked_ReturnsNull()
        {
            var agvGraphTextFixtureMock = new Mock<AgvGraphTestFixture>();
            agvGraphTextFixtureMock.Setup(method => method.GetAllPossibleRoutesOrdered())
                .Returns(new[]
                {
                    new[] { "Saarbrücken", "Kaiserslautern", "Frankfurt", "Würzburg" },
                    new[] { "Saarbrücken", "Kaiserslautern", "Ludwigshafen", "Würzburg" },
                    new[] { "Saarbrücken", "Karlsruhe", "Heilbronn", "Würzburg" }
                });
            var agvGraphTextFixture = agvGraphTextFixtureMock.Object;

            AssertFindsShortestPath("Saarbrücken", "Würzburg", agvGraphTextFixture);
        }

        [TestMethod]
        public void OnlyOnePossibleRoute_FindsRoute_WhenBlockedEdgeDoesNotFindRoute()
        {
            var agvGraphTextFixtureMock = new Mock<AgvGraphTestFixture>();
            agvGraphTextFixtureMock.Setup(method => method.GetAllPossibleRoutesOrdered())
                .Returns(new[]
                {
                    new[] { "Saarbrücken", "LoneRoute", "LoneDestination" }
                });
            var agvGraphTextFixture = agvGraphTextFixtureMock.Object;

            AssertFindsShortestPath("Saarbrücken", "LoneDestination", agvGraphTextFixture);
        }

        private void AssertFindsShortestPath(string source, string destination, AgvGraphTestFixture agvGraphTextFixture)
        {
            var aStar = new AStar(agvGraphTextFixture.Graph, RoutingPlausibilityChecker);
            var route = aStar.CalculateRoute(source, destination);

            route.ShouldNotBeNull();
            route.Nodes.ShouldBe(agvGraphTextFixture.GetAllPossibleRoutesOrdered().First());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void TestStaticCalculateRoute_FindsRoute_And_NoRoute(bool isEdgePlausible)
        {
            var graph = new AgvGraph(Guid.Empty,
                new GraphNode[]
                {
                    new("1") { Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 5f, Z = 0f },
                    new("2") { Type = AgvGraph.NodeType.PickDrop, X = 1f, Y = 5f, Z = 0f },
                    new("3") { Type = AgvGraph.NodeType.PickDrop, X = 1f, Y = 2f, Z = 0f },
                    new("4") { Type = AgvGraph.NodeType.PickDrop, X = 3f, Y = 3f, Z = 0f },
                    new("5") { Type = AgvGraph.NodeType.PickDrop, X = 3f, Y = 6f, Z = 0f },
                    new("6") { Type = AgvGraph.NodeType.PickDrop, X = 3f, Y = 2f, Z = 0f },
                    new("7") { Type = AgvGraph.NodeType.PickDrop, X = 6f, Y = 0f, Z = 0f },
                    new("8") { Type = AgvGraph.NodeType.PickDrop, X = 7f, Y = 1f, Z = 0f },
                    new("9") { Type = AgvGraph.NodeType.PickDrop, X = 8f, Y = 2f, Z = 0f },
                    new("10") { Type = AgvGraph.NodeType.PickDrop, X = 8f, Y = 5f, Z = 0f }
                },
                new GraphEdge[]
                {
                    new("1", "2", 1f),
                    new("1", "4", 4f),
                    new("4", "1", 4f),
                    new("2", "5", 3f),
                    new("3", "5", 5f),
                    new("3", "4", 3f),
                    new("4", "3", 3f),
                    new("3", "6", 2f),
                    new("6", "3", 2f),
                    new("5", "8", 7f),
                    new("8", "5", 7f),
                    new("8", "7", 1f),
                    new("7", "8", 1f),
                    new("9", "8", 1f),
                    new("7", "10", 6f)
                },
                "Test".ToGuid(),
                "Test");

            var sourceNode = graph.Nodes.First(x => x.Id == "1");
            var destinationNode = graph.Nodes.First(x => x.Id == "8");
            
            var routingPlausibilityChecker = new Mock<IRoutingPlausibilityChecker>();
            routingPlausibilityChecker.Setup(x => x.IsPlausible(sourceNode, destinationNode)).Returns(isEdgePlausible);
            var aStar = new AStar(graph, routingPlausibilityChecker.Object);
            var route = aStar.CalculateRoute(sourceNode.Id, destinationNode.Id);
            bool hasRoute = route != null;
            hasRoute.ShouldBe(isEdgePlausible);
        }
    }
}