using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;


namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// Test for utility <see cref="AStar"/>.
    /// </summary>
    [TestClass]
    public class AStarTest
    {
        private readonly AgvGraph? _graph;
        private readonly AgvGraph? _grid;


        /// <summary>
        /// Test initialization.
        /// </summary>
        public AStarTest()
        {
            _graph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node("1") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 0f, Y = 5f, Z = 0f },
                    new AgvGraph.Node("2") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 1f, Y = 5f, Z = 0f },
                    new AgvGraph.Node("3") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 1f, Y = 2f, Z = 0f },
                    new AgvGraph.Node("4") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 3f, Y = 3f, Z = 0f },
                    new AgvGraph.Node("5") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 3f, Y = 6f, Z = 0f },
                    new AgvGraph.Node("6") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 3f, Y = 2f, Z = 0f },
                    new AgvGraph.Node("7") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 6f, Y = 0f, Z = 0f },
                    new AgvGraph.Node("8") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 7f, Y = 1f, Z = 0f },
                    new AgvGraph.Node("9") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 8f, Y = 2f, Z = 0f },
                    new AgvGraph.Node("10") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 8f, Y = 5f, Z = 0f }
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
                });

            var map = new List<string>
            { // --> x [1..50]
              // 12345678901234567890123456789012345678901234567890
                "           xxxxxx    xxx    xxx   xx  xx  x x x x ",  // 01 |
                "        x                                         ",  // 02 |
                "        x                                         ",  // 03 v
                "    xxxxx                                         ",  // 04
                "        x  ooooooooooooooooooooooo                ",  // 05 y [1..25}
                "        x  o   o   o   o   o   o o                ",  // 06
                "           o o o o o o o o o o o o                ",  // 07
                "           o o o o o o o o o o o o                ",  // 08
                "           o o o o o o o o o o o o                ",  // 09
                "           o o o o o o o o o o o o                ",  // 10
                "           o o o o o o o o o o o o                ",  // 11
                "           o o o o o o o o o o o o                ",  // 12
                "           o o o o o o o o o o o o                ",  // 13
                "           o o o o o o o o o o o o                ",  // 14
                "           o o o o o o o o o o o o                ",  // 15
                "           o o o o o o o o o o o o                ",  // 16
                "           o o   o   o   o   o   o                ",  // 17
                "           o ooooooooooooooooooooo                ",  // 18
                "                                                  ",  // 19
                "                                                  ",  // 20
                "                                                  ",  // 21
                "                                            xxxxx ",  // 22
                "                                            x   x ",  // 23
                "                                            xxxxx ",  // 24
                "                                                  "   // 25
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
            _grid = new AgvGraph(Guid.Empty, nodes.ToArray(), edges.ToArray());
        }


        /// <summary>
        /// Test sorting of <see cref="Common.Definitions.IGraphAlgorithm.Route"/>.
        /// </summary>
        [TestMethod]
        public void Test_RouteComparable()
        {
            var routes = new List<Common.Definitions.IGraphAlgorithm.Route?>
            {
                new() { Costs = 9f },
                new() { Costs = 1f },
                null,
                new() { Costs = 5f }
            };

            List<Common.Definitions.IGraphAlgorithm.Route?> sorted = routes.OrderByDescending(n => n != null).ThenBy(n => n).ToList();
            Assert.AreEqual(routes[1]!.Costs, sorted[0]!.Costs, 0.00001f);
            Assert.AreEqual(routes[3]!.Costs, sorted[1]!.Costs, 0.00001f);
            Assert.AreEqual(routes[0]!.Costs, sorted[2]!.Costs, 0.00001f);
            Assert.IsTrue(sorted[3] == null);
        }

        /// <summary>
        /// Test AStar implementation with example from Wikipedia.
        /// </summary>
        [TestMethod]
        public void Test_AStar_WikipediaExample()
        {
            var graph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node("Saarbrücken") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 0f, Y = 0f, Z = 0f },
                    new AgvGraph.Node("Kaiserslautern") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 42f, Y = 80f, Z = 0f },
                    new AgvGraph.Node("Frankfurt") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 104f, Y = 100f, Z = 0f },
                    new AgvGraph.Node("Ludwigshafen") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 96f, Y = 60f, Z = 0f },
                    new AgvGraph.Node("Karlsruhe") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 104f, Y = -10f, Z = 0f },
                    new AgvGraph.Node("Heilbronn") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 150f, Y = 20f, Z = 0f },
                    new AgvGraph.Node("Würzburg") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 200f, Y = 92f, Z = 0f }
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
                });
            var aStar = new AStar(graph);

            float? costs = aStar.Costs("Saarbrücken", "Würzburg");
            Assert.AreEqual(289f, costs!.Value, 0.00001f);
        }

        /// <summary>
        /// Test AStar implementation with am example having bidirectional edges.
        /// </summary>
        [TestMethod]
        public void Test_AStarBidirectional_UseGraphSeveralTimes()
        {
            var aStar = new AStar(_graph!);

            float? costs = aStar.Costs("1", "10");
            Assert.AreEqual(18f, costs!.Value, 0.00001f);

            costs = aStar.Costs("10", "1");
            Assert.IsFalse(costs.HasValue);

            costs = aStar.Costs("9", "5");
            Assert.AreEqual(8f, costs!.Value, 0.00001f);

            costs = aStar.Costs("4", "9");
            Assert.IsFalse(costs.HasValue);
        }

        /// <summary>
        /// Intensiv path test.
        /// </summary>
        [TestMethod]
        public void Test_AStar_GridExample()
        {
            var aStar = new AStar(_grid!);
            // ReSharper disable JoinDeclarationAndInitializer
            float? c1, c2, c3, c4, costs;
            // ReSharper restore JoinDeclarationAndInitializer

            c1 = aStar.Costs("01/01", "11/01");
            Assert.AreEqual(10f, c1!.Value, 0.00001f);
            c2 = aStar.Costs("11/01", "11/02");
            Assert.AreEqual(1f, c2!.Value, 0.00001f);
            c3 = aStar.Costs("11/02", "50/02");
            Assert.AreEqual(39f, c3!.Value, 0.00001f);
            c4 = aStar.Costs("50/02", "50/01");
            Assert.AreEqual(1f, c4!.Value, 0.00001f);

            costs = aStar.Costs("01/01", "50/01");
            Assert.AreEqual(c1.Value + c2.Value + c3.Value + c4.Value, costs!.Value, 0.00001f);

            c1 = aStar.Costs("50/01", "13/17");
            Assert.AreEqual(57f, c1!.Value, 0.00001f);
            c2 = aStar.Costs("13/17", "33/17");
            Assert.AreEqual(10f * 13f, c2!.Value, 0.00001f);
            c3 = aStar.Costs("33/17", "33/06");
            Assert.AreEqual(11f, c3!.Value, 0.00001f);

            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            costs = aStar.Costs("50/01", "33/06");
            stopWatch.Stop();
            System.Diagnostics.Trace.WriteLine("Grid example duration: " + stopWatch.ElapsedMilliseconds.ToString() + "ms.");
            Assert.AreEqual(c1.Value + c2.Value + c3.Value, costs!.Value, 0.00001f);
        }

        /// <summary>
        /// Test path to non existent node.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void Test_AStarGrid_NonExistentNode_ExpectException()
        {
            var aStar = new AStar(_grid!);
            aStar.Costs("01/01", "49/24");
        }

        /// <summary>
        /// Test path to unreachable node.
        /// </summary>
        [TestMethod]
        public void Test_AStarGrid_UnreachableNode_NoPath()
        {
            var aStar = new AStar(_grid!);
            float? costs = aStar.Costs("01/01", "47/23");
            Assert.IsFalse(costs.HasValue);
        }
    }
}
