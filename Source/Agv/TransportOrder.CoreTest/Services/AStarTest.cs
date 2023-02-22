using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
{
    /// <summary>
    /// Test for utility <see cref="AStar"/>.
    /// </summary>
    [TestClass]
    public class AStarTest
    {
        private readonly AgvGraph? _graph;
        private readonly AgvGraph? _grid;
        private readonly Mock<IRoutingPlausibilityChecker> _routingPlausibilityCheckerMock;

        private IRoutingPlausibilityChecker RoutingPlausibilityChecker => _routingPlausibilityCheckerMock.Object;

        public AStarTest()
        {
            const string mapName = "Map#1";
            _graph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node("1") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 5f, Z = 0f },
                    new AgvGraph.Node("2") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 1f, Y = 5f, Z = 0f },
                    new AgvGraph.Node("3") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 1f, Y = 2f, Z = 0f },
                    new AgvGraph.Node("4") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 3f, Y = 3f, Z = 0f },
                    new AgvGraph.Node("5") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 3f, Y = 6f, Z = 0f },
                    new AgvGraph.Node("6") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 3f, Y = 2f, Z = 0f },
                    new AgvGraph.Node("7") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 6f, Y = 0f, Z = 0f },
                    new AgvGraph.Node("8") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 7f, Y = 1f, Z = 0f },
                    new AgvGraph.Node("9") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 8f, Y = 2f, Z = 0f },
                    new AgvGraph.Node("10") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 8f, Y = 5f, Z = 0f }
                },
                new[]
                {
                    new AgvGraph.Edge("1", "2") { Metric = 1f },
                    new AgvGraph.Edge("1", "4") { Metric = 4f },
                    new AgvGraph.Edge("4", "1") { Metric = 4f },
                    new AgvGraph.Edge("2", "5") { Metric = 3f },
                    new AgvGraph.Edge("3", "5") { Metric = 5f },
                    new AgvGraph.Edge("3", "4") { Metric = 3f },
                    new AgvGraph.Edge("4", "3") { Metric = 3f },
                    new AgvGraph.Edge("3", "6") { Metric = 2f },
                    new AgvGraph.Edge("6", "3") { Metric = 2f },
                    new AgvGraph.Edge("5", "8") { Metric = 7f },
                    new AgvGraph.Edge("8", "5") { Metric = 7f },
                    new AgvGraph.Edge("8", "7") { Metric = 1f },
                    new AgvGraph.Edge("7", "8") { Metric = 1f },
                    new AgvGraph.Edge("9", "8") { Metric = 1f },
                    new AgvGraph.Edge("7", "10") { Metric = 6f }
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
            var nodes = new List<AgvGraph.Node>();
            var edges = new List<AgvGraph.Edge>();
            int x, y = 1;
            foreach (string line in map)
            {
                x = 1;
                foreach (char c in line)
                {
                    if (c == ' ')
                    {
                        var next = new AgvGraph.Node($"{x:D2}/{y:D2}") { X = x, Y = y, Z = 0f, Type = AgvGraph.NodeType.WayPoint };
                        nodes.Add(next);
                        var prev = nodes.SingleOrDefault(n => n.Id == $"{x - 1:D2}/{y:D2}");
                        if (prev != null)
                        {
                            edges.Add(new AgvGraph.Edge(prev.Id, next.Id) { Metric = 1f });
                            edges.Add(new AgvGraph.Edge(next.Id, prev.Id) { Metric = 1f });
                        }
                        prev = nodes.SingleOrDefault(n => n.Id == $"{x:D2}/{y - 1:D2}");
                        if (prev != null)
                        {
                            edges.Add(new AgvGraph.Edge(prev.Id, next.Id) { Metric = 1f });
                            edges.Add(new AgvGraph.Edge(next.Id, prev.Id) { Metric = 1f });
                        }
                    }
                    ++x;
                }
                ++y;
            }
            _grid = new AgvGraph(Guid.Empty, nodes.ToArray(), edges.ToArray(), mapName.ToGuid(), mapName);

            _routingPlausibilityCheckerMock = new Mock<IRoutingPlausibilityChecker>();
            _routingPlausibilityCheckerMock.Setup(c => c.IsPlausible(It.IsAny<AgvGraph.Node>(), It.IsAny<AgvGraph.Node>()))
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
            const string mapName = "Map#1";
            var graph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node("Saarbrücken") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f },
                    new AgvGraph.Node("Kaiserslautern") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 42f, Y = 80f, Z = 0f },
                    new AgvGraph.Node("Frankfurt") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = 100f, Z = 0f },
                    new AgvGraph.Node("Ludwigshafen") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 96f, Y = 60f, Z = 0f },
                    new AgvGraph.Node("Karlsruhe") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = -10f, Z = 0f },
                    new AgvGraph.Node("Heilbronn") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 150f, Y = 20f, Z = 0f },
                    new AgvGraph.Node("Würzburg") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 200f, Y = 92f, Z = 0f }
                },
                new[]
                {
                    new AgvGraph.Edge("Saarbrücken", "Kaiserslautern") { Metric = 70f },
                    new AgvGraph.Edge("Saarbrücken", "Karlsruhe") { Metric = 145f },
                    new AgvGraph.Edge("Kaiserslautern", "Frankfurt") { Metric = 103f },
                    new AgvGraph.Edge("Kaiserslautern", "Ludwigshafen") { Metric = 53f },
                    new AgvGraph.Edge("Karlsruhe", "Heilbronn") { Metric = 84f },
                    new AgvGraph.Edge("Frankfurt", "Würzburg") { Metric = 116f },
                    new AgvGraph.Edge("Ludwigshafen", "Würzburg") { Metric = 183f },
                    new AgvGraph.Edge("Heilbronn", "Würzburg") { Metric = 102f }
                },
                mapName.ToGuid(),
                mapName);
            var aStar = new AStar(graph, RoutingPlausibilityChecker);

            var route = aStar.CalculateRoute("Saarbrücken", "Würzburg");
            route.ShouldNotBeNull();
            route.Nodes.ShouldBe(new[] { "Saarbrücken", "Kaiserslautern", "Frankfurt", "Würzburg" });
            route.Costs.ShouldBe(289f, 0.00001f);
        }

        [TestMethod]
        public void Test_AStar_WikipediaExample_WithCurrentNode()
        {
            const string mapName = "Map#1";
            var graph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node("Saarbrücken") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f },
                    new AgvGraph.Node("Kaiserslautern") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 42f, Y = 80f, Z = 0f },
                    new AgvGraph.Node("Frankfurt") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = 100f, Z = 0f },
                    new AgvGraph.Node("Ludwigshafen") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 96f, Y = 60f, Z = 0f },
                    new AgvGraph.Node("Karlsruhe") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = -10f, Z = 0f },
                    new AgvGraph.Node("Heilbronn") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 150f, Y = 20f, Z = 0f },
                    new AgvGraph.Node("Würzburg") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 200f, Y = 92f, Z = 0f }
                },
                new[]
                {
                    new AgvGraph.Edge("Saarbrücken", "Kaiserslautern") { Metric = 70f },
                    new AgvGraph.Edge("Saarbrücken", "Karlsruhe") { Metric = 145f },
                    new AgvGraph.Edge("Karlsruhe", "Saarbrücken") { Metric = 145f },
                    new AgvGraph.Edge("Kaiserslautern", "Frankfurt") { Metric = 103f },
                    new AgvGraph.Edge("Kaiserslautern", "Ludwigshafen") { Metric = 53f },
                    new AgvGraph.Edge("Karlsruhe", "Heilbronn") { Metric = 84f },
                    new AgvGraph.Edge("Frankfurt", "Würzburg") { Metric = 116f },
                    new AgvGraph.Edge("Ludwigshafen", "Würzburg") { Metric = 183f },
                    new AgvGraph.Edge("Heilbronn", "Würzburg") { Metric = 102f }
                },
                mapName.ToGuid(),
                mapName);
            var aStar = new AStar(graph, RoutingPlausibilityChecker);

            var route = aStar.CalculateRoute("Karlsruhe", "Saarbrücken", "Würzburg");
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
                    new AgvGraph.Node("1") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 5f, Z = 0f },
                    new AgvGraph.Node("2") { Enabled = true, Type = AgvGraph.NodeType.Drop, X = 1f, Y = 5f, Z = 0f }
                },
                new[]
                {
                    new AgvGraph.Edge("1", "2") { Metric = 1f },
                    new AgvGraph.Edge("2", "1") { Metric = 4f }
                },
                mapName.ToGuid(),
                mapName);

            var routingPlausibilityCheckerMock = new Mock<IRoutingPlausibilityChecker>();
            routingPlausibilityCheckerMock.Setup(r => r.IsPlausible(It.IsAny<AgvGraph.Node>(), It.IsAny<AgvGraph.Node>())).Returns(true);

            var aStar = new AStar(graph, routingPlausibilityCheckerMock.Object);
            var route = aStar.CalculateRoute("1", "2");
            route.ShouldNotBeNull();

            routingPlausibilityCheckerMock.Setup(r => r.IsPlausible(It.IsAny<AgvGraph.Node>(), It.IsAny<AgvGraph.Node>())).Returns(false);
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
            AssertWhenEdgeBlock_FindsAlternativePath("Saarbrücken", "Würzburg", agvGraphTextFixture);
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
            AssertWhenEdgeBlock_FindsAlternativePath("Saarbrücken", "LoneDestination", agvGraphTextFixture);
        }

        private void AssertFindsShortestPath(string source, string destination, AgvGraphTestFixture agvGraphTextFixture)
        {
            var aStar = new AStar(agvGraphTextFixture.Graph, RoutingPlausibilityChecker);
            var route = aStar.CalculateRoute(source, destination);

            route.ShouldNotBeNull();
            route.Nodes.ShouldBe(agvGraphTextFixture.GetAllPossibleRoutesOrdered().First());
        }

        private void AssertWhenEdgeBlock_FindsAlternativePath(string source, string destination, AgvGraphTestFixture agvGraphTextFixture)
        {
            string[][] possibleRoutesOrdered = agvGraphTextFixture.GetAllPossibleRoutesOrdered();
            for (int routeNum = 0; routeNum < possibleRoutesOrdered.Length; routeNum++)
            {
                // remove node of possible route one by one until there is none anymore
                agvGraphTextFixture.BlockEdgeInRoute(possibleRoutesOrdered[routeNum]);
                var aStar = new AStar(agvGraphTextFixture.Graph, RoutingPlausibilityChecker);
                var route = aStar.CalculateRoute(source, destination);

                if (routeNum != possibleRoutesOrdered.Length - 1)
                {
                    route.ShouldNotBeNull();
                    route.Nodes.ShouldBe(possibleRoutesOrdered[routeNum + 1]);
                }
                else
                {
                    route.ShouldBeNull();
                }
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void TestStaticCalculateRoute_FindsRoute_And_NoRoute(bool blockEdgesThatLeadToNoRoute)
        {
            var graph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node("1") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 5f, Z = 0f },
                    new AgvGraph.Node("2") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 1f, Y = 5f, Z = 0f },
                    new AgvGraph.Node("3") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 1f, Y = 2f, Z = 0f },
                    new AgvGraph.Node("4") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 3f, Y = 3f, Z = 0f },
                    new AgvGraph.Node("5") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 3f, Y = 6f, Z = 0f },
                    new AgvGraph.Node("6") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 3f, Y = 2f, Z = 0f },
                    new AgvGraph.Node("7") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 6f, Y = 0f, Z = 0f },
                    new AgvGraph.Node("8") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 7f, Y = 1f, Z = 0f },
                    new AgvGraph.Node("9") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 8f, Y = 2f, Z = 0f },
                    new AgvGraph.Node("10") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 8f, Y = 5f, Z = 0f }
                },
                new[]
                {
                    new AgvGraph.Edge("1", "2", blockEdgesThatLeadToNoRoute) { Metric = 1f },
                    new AgvGraph.Edge("1", "4", blockEdgesThatLeadToNoRoute) { Metric = 4f },
                    new AgvGraph.Edge("4", "1") { Metric = 4f },
                    new AgvGraph.Edge("2", "5", blockEdgesThatLeadToNoRoute) { Metric = 3f },
                    new AgvGraph.Edge("3", "5", blockEdgesThatLeadToNoRoute) { Metric = 5f },
                    new AgvGraph.Edge("3", "4", blockEdgesThatLeadToNoRoute) { Metric = 3f },
                    new AgvGraph.Edge("4", "3") { Metric = 3f },
                    new AgvGraph.Edge("3", "6", blockEdgesThatLeadToNoRoute) { Metric = 2f },
                    new AgvGraph.Edge("6", "3") { Metric = 2f },
                    new AgvGraph.Edge("5", "8", blockEdgesThatLeadToNoRoute) { Metric = 7f },
                    new AgvGraph.Edge("8", "5") { Metric = 7f },
                    new AgvGraph.Edge("8", "7", blockEdgesThatLeadToNoRoute) { Metric = 1f },
                    new AgvGraph.Edge("7", "8") { Metric = 1f },
                    new AgvGraph.Edge("9", "8", blockEdgesThatLeadToNoRoute) { Metric = 1f },
                    new AgvGraph.Edge("7", "10") { Metric = 6f }
                },
                "Test".ToGuid(),
                "Test");
            var route = AStar.CalculateRouteStatic(graph, "1", "8");
            bool routeNull = route == null;
            routeNull.ShouldBe(blockEdgesThatLeadToNoRoute);
        }
    }
}