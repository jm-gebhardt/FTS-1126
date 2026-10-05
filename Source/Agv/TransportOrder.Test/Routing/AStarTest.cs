using System.Diagnostics;
using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Routing.Base;
using Gebhardt.Acc.TransportOrder.Routing.Implementations;
using Gebhardt.Acc.TransportOrder.Routing.Models;
using Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Routing;

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

    private readonly AgvData _agvData = AgvDataGenerator.GenerateAgvData(acceleration: 1, maxVelocityStraight: 1, maxVelocityTurn: 0.9, maxVelocityTurnOnSpot: Math.PI, turnOnSpotThreshold: 0.08726646);
    private readonly AgvData _slowTurnAgvData = AgvDataGenerator.GenerateAgvData(acceleration: 1, maxVelocityStraight: 1, maxVelocityTurn: 0.9, maxVelocityTurnOnSpot: 0.0174532, turnOnSpotThreshold: 0.08726646);

    private IRoutingPlausibilityChecker RoutingPlausibilityChecker => _routingPlausibilityCheckerMock.Object;

    public AStarTest()
    {
        _graph = AgvGraphGenerator.Generate(
            new[]
            {
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 0f, 5f, nodeName: "1"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 1f, 5f, nodeName: "2"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, nodeName: "3"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 3f, 3f, nodeName: "4"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 3f, 6f, nodeName: "5"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 3f, nodeName: "6"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 6f, 0f, nodeName: "7"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 7f, 1f, nodeName: "8"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 8f, nodeName: "9"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 8f, 5f, nodeName: "10")
            },
            new[]
            {
                GraphEdge.CreateDirect("1".ToGuid(), "2".ToGuid()),
                GraphEdge.CreateDirect("1".ToGuid(), "4".ToGuid(), 4f),
                GraphEdge.CreateDirect("4".ToGuid(), "1".ToGuid(), 4f),
                GraphEdge.CreateDirect("2".ToGuid(), "5".ToGuid(), 3f),
                GraphEdge.CreateDirect("3".ToGuid(), "5".ToGuid(), 5f),
                GraphEdge.CreateDirect("3".ToGuid(), "4".ToGuid(), 3f),
                GraphEdge.CreateDirect("4".ToGuid(), "3".ToGuid(), 3f),
                GraphEdge.CreateDirect("3".ToGuid(), "6".ToGuid(), 2f),
                GraphEdge.CreateDirect("6".ToGuid(), "3".ToGuid(), 2f),
                GraphEdge.CreateDirect("5".ToGuid(), "8".ToGuid(), 7f),
                GraphEdge.CreateDirect("8".ToGuid(), "5".ToGuid(), 7f),
                GraphEdge.CreateDirect("8".ToGuid(), "7".ToGuid()),
                GraphEdge.CreateDirect("7".ToGuid(), "8".ToGuid()),
                GraphEdge.CreateDirect("9".ToGuid(), "8".ToGuid()),
                GraphEdge.CreateDirect("7".ToGuid(), "10".ToGuid(), 6f)
            });

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
                    var next = NodeGenerator.GenerateNode(AgvGraph.NodeType.WayPoint, x, y, nodeName: $"{x:D2}/{y:D2}");
                    nodes.Add(next);
                    var prev = nodes.SingleOrDefault(n => n.Name == $"{x - 1:D2}/{y:D2}");
                    if (prev != null)
                    {
                        edges.Add(GraphEdge.CreateDirect(prev.Name.ToGuid(), next.Name.ToGuid()));
                        edges.Add(GraphEdge.CreateDirect(next.Name.ToGuid(), prev.Name.ToGuid()));
                    }
                    prev = nodes.SingleOrDefault(n => n.Name == $"{x:D2}/{y - 1:D2}");
                    if (prev != null)
                    {
                        edges.Add(GraphEdge.CreateDirect(prev.Name.ToGuid(), next.Name.ToGuid()));
                        edges.Add(GraphEdge.CreateDirect(next.Name.ToGuid(), prev.Name.ToGuid()));
                    }
                }
                ++x;
            }
            ++y;
        }
        _grid = AgvGraphGenerator.Generate(nodes, edges);

        _wikiExample = AgvGraphGenerator.Generate(
            [
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 0f, 0f, nodeName: "Nairobi"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 42f, 80f, nodeName: "Tokyo"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 104f, 100f, nodeName: "Marseille"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 96f, 60f, nodeName: "Berlin"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 104f, -10f, nodeName: "Denver"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 150f, 20f, nodeName: "Helsinki"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 200f, 92f, nodeName: "Rio")
            ],
            new[]
            {
                GraphEdge.CreateDirect("Nairobi".ToGuid(), "Tokyo".ToGuid(), 70f),
                GraphEdge.CreateDirect("Nairobi".ToGuid(), "Denver".ToGuid(), 145f),
                GraphEdge.CreateDirect("Denver".ToGuid(), "Nairobi".ToGuid(), 145f),
                GraphEdge.CreateDirect("Tokyo".ToGuid(), "Marseille".ToGuid(), 103f),
                GraphEdge.CreateDirect("Tokyo".ToGuid(), "Berlin".ToGuid(), 53f),
                GraphEdge.CreateDirect("Denver".ToGuid(), "Helsinki".ToGuid(), 84f),
                GraphEdge.CreateDirect("Marseille".ToGuid(), "Rio".ToGuid(), 116f),
                GraphEdge.CreateDirect("Berlin".ToGuid(), "Rio".ToGuid(), 183f),
                GraphEdge.CreateDirect("Helsinki".ToGuid(), "Rio".ToGuid(), 102f)
            });

        _routingPlausibilityCheckerMock = new Mock<IRoutingPlausibilityChecker>();
        _routingPlausibilityCheckerMock.Setup(c => c.IsPlausible(It.IsAny<GraphNode>(), It.IsAny<GraphNode>()))
            .Returns(true);
    }

    /// <summary>
    /// Test sorting of <see cref="Route"/>.
    /// </summary>
    [TestMethod]
    public void Test_RouteComparable()
    {
        var routes = new List<Route?>
        {
            new(9f, [], []),
            new(1f, [], []),
            null,
            new(5f, [], [])
        };

        var sorted = routes.OrderByDescending(n => n != null).ThenBy(n => n).ToList();
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
        var aStar = new AStar(_wikiExample, _agvData, RoutingPlausibilityChecker);

        var route = aStar.CalculateRoute("Nairobi".ToGuid(), "Rio".ToGuid());
        route.ShouldNotBeNull();
        route.Nodes.Select(n => n.Name).ShouldBe(["Nairobi", "Tokyo", "Marseille", "Rio"]);
        route.Costs.ShouldBe(291.8725786, 0.00001f);
    }

    [TestMethod]
    public void Test_AStar_WikipediaExample_SameSourceDestination()
    {
        var aStar = new AStar(_wikiExample, null, RoutingPlausibilityChecker);

        var route = aStar.CalculateRoute("Nairobi".ToGuid(), "Nairobi".ToGuid());
        route.ShouldNotBeNull();
        route.Nodes.Select(n => n.Name).ShouldBe(new[] { "Nairobi" });
        route.Costs.ShouldBe(0f, 0.00001f);
    }

    [TestMethod]
    public void Test_AStar_WikipediaExample_WithCurrentNode()
    {
        var aStar = new AStar(_wikiExample, _agvData, RoutingPlausibilityChecker);

        var route = aStar.CalculateRoute("Denver".ToGuid(), "Nairobi".ToGuid(), "Rio".ToGuid());
        route.ShouldNotBeNull();
        route.Nodes.Select(n => n.Name).ShouldBe(new[] { "Denver", "Nairobi", "Tokyo", "Marseille", "Rio" });
        route.Costs.ShouldBe(289 + 145 + 3.3725786, 0.00001f);
    }

    [TestMethod]
    public void Test_AStar_WikipediaExample_MultipleWaypoints()
    {
        var aStar = new AStar(_wikiExample, _agvData, RoutingPlausibilityChecker);

        var waypoints = new[] { "Denver".ToGuid(), "Nairobi".ToGuid(), "Nairobi".ToGuid(), "Rio".ToGuid() }; // (Denver => Nairobi) + (Nairobi => Rio)

        var route = aStar.CalculateRoute(waypoints);
        route.ShouldNotBeNull();
        route.Nodes.Select(n => n.Name).ShouldBe(new[] { "Denver", "Nairobi", "Tokyo", "Marseille", "Rio" });
        route.Costs.ShouldBe(289 + 145 + 3.3725786, 0.00001f);
    }

    /// <summary>
    /// Test AStar implementation with an example having bidirectional edges.
    /// </summary>
    [TestMethod]
    public void Test_AStarBidirectional_UseGraphSeveralTimes()
    {
        var aStar = new AStar(_graph!, _agvData, RoutingPlausibilityChecker);

        var route = aStar.CalculateRoute("1".ToGuid(), "10".ToGuid());
        route.ShouldNotBeNull();
        route.Costs.ShouldBe(18f + 6.416286f, 0.00001f);

        route = aStar.CalculateRoute("10".ToGuid(), "1".ToGuid());
        route.ShouldBeNull();

        route = aStar.CalculateRoute("9".ToGuid(), "5".ToGuid());
        route.ShouldNotBeNull();
        route.Costs.ShouldBe(8 + 2.03522328, 0.00001f);

        route = aStar.CalculateRoute("4".ToGuid(), "9".ToGuid());
        route.ShouldBeNull();
    }

    /// <summary>
    /// Intensive path test.
    /// </summary>
    [TestMethod]
    public void Test_AStar_GridExample()
    {
        var aStar = new AStar(_grid!, _agvData, RoutingPlausibilityChecker);
        // ReSharper disable JoinDeclarationAndInitializer
        Route? c1, c2, c3, c4, costs;
        // ReSharper restore JoinDeclarationAndInitializer

        c1 = aStar.CalculateRoute("01/01".ToGuid(), "11/01".ToGuid());
        c1!.Costs.ShouldBe(10.5f, 0.00001f);
        c2 = aStar.CalculateRoute("11/01".ToGuid(), "11/02".ToGuid());
        c2!.Costs.ShouldBe(1.5f, 0.00001f);
        c3 = aStar.CalculateRoute("11/02".ToGuid(), "50/02".ToGuid());
        c3!.Costs.ShouldBe(39.5f, 0.00001f);
        c4 = aStar.CalculateRoute("50/02".ToGuid(), "50/01".ToGuid());
        c4!.Costs.ShouldBe(1.5f, 0.00001f);

        costs = aStar.CalculateRoute("01/01".ToGuid(), "50/01".ToGuid());
        costs!.Costs.ShouldBe(c1.Costs + c2.Costs + c3.Costs + c4.Costs + 3, 0.00001f);

        c1 = aStar.CalculateRoute("50/01".ToGuid(), "13/17".ToGuid());
        c1!.Costs.ShouldBe(60.5f, 0.00001f);
        c2 = aStar.CalculateRoute("13/17".ToGuid(), "33/17".ToGuid());
        c2!.Costs.ShouldBe(10f * 13f + 29f, 0.00001f);
        c3 = aStar.CalculateRoute("33/17".ToGuid(), "33/06".ToGuid());
        c3!.Costs.ShouldBe(11.5f, 0.00001f);

        var stopWatch = new Stopwatch();
        stopWatch.Start();
        costs = aStar.CalculateRoute("50/01".ToGuid(), "33/06".ToGuid());
        stopWatch.Stop();
        Trace.WriteLine("Grid example duration: " + stopWatch.ElapsedMilliseconds + "ms.");
        costs!.Costs.ShouldBe(c1.Costs + c2.Costs + c3.Costs + 0.5, 0.00001f);
    }

    /// <summary>
    /// Test path to or from not existent node.
    /// </summary>
    [TestMethod]
    [DataRow("01/01", "XX/XX")]
    [DataRow("YY/YY", "01/01")]
    [DataRow("YY/YY", "XX/XX")]
    public void Test_AStarGrid_NonExistentSourceOrDestination_NoPath(string source, string destination)
    {
        var aStar = new AStar(_grid!, null, RoutingPlausibilityChecker);
        aStar.CalculateRoute(source.ToGuid(), destination.ToGuid()).ShouldBeNull();
    }

    /// <summary>
    /// Test path to unreachable node.
    /// </summary>
    [TestMethod]
    public void Test_AStarGrid_UnreachableNode_NoPath()
    {
        var aStar = new AStar(_grid!, null, RoutingPlausibilityChecker);
        var route = aStar.CalculateRoute("01/01".ToGuid(), "47/23".ToGuid());
        route.ShouldBeNull();
    }

    [TestMethod]
    public void CalculateRoute_ReturnsNull_WhenRouteNotPlausible()
    {
        var graph = AgvGraphGenerator.Generate(
            [
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 0f, 5f, nodeName: "1"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.Drop, 1f, 5f, nodeName: "2")
            ],
            new[]
            {
                GraphEdge.CreateDirect("1".ToGuid(), "2".ToGuid()),
                GraphEdge.CreateDirect("2".ToGuid(), "1".ToGuid(), 4f)
            });

        var routingPlausibilityCheckerMock = new Mock<IRoutingPlausibilityChecker>();
        routingPlausibilityCheckerMock.Setup(r => r.IsPlausible(It.IsAny<GraphNode>(), It.IsAny<GraphNode>())).Returns(true);

        var aStar = new AStar(graph, null, routingPlausibilityCheckerMock.Object);
        var route = aStar.CalculateRoute("1".ToGuid(), "2".ToGuid());
        route.ShouldNotBeNull();

        routingPlausibilityCheckerMock.Setup(r => r.IsPlausible(It.IsAny<GraphNode>(), It.IsAny<GraphNode>())).Returns(false);
        aStar = new AStar(graph, null, routingPlausibilityCheckerMock.Object);
        route = aStar.CalculateRoute("1".ToGuid(), "2".ToGuid());
        route.ShouldBeNull();
    }

    [TestMethod]
    public void FindsShortestPossibleRoute_WhenAllPossibleRoutesBlocked_ReturnsNull()
    {
        var agvGraphTextFixtureMock = new Mock<AgvGraphTestData>();
        agvGraphTextFixtureMock.Setup(method => method.GetAllPossibleRoutesOrdered())
            .Returns(new[]
            {
                new[] { "Nairobi", "Tokyo", "Marseille", "Rio" },
                new[] { "Nairobi", "Tokyo", "Berlin", "Rio" },
                new[] { "Nairobi", "Denver", "Helsinki", "Rio" }
            });
        var agvGraphTextFixture = agvGraphTextFixtureMock.Object;

        AssertFindsShortestPath("Nairobi", "Rio", agvGraphTextFixture);
    }

    [TestMethod]
    public void OnlyOnePossibleRoute_FindsRoute_WhenBlockedEdgeDoesNotFindRoute()
    {
        var agvGraphTextFixtureMock = new Mock<AgvGraphTestData>();
        agvGraphTextFixtureMock.Setup(method => method.GetAllPossibleRoutesOrdered())
            .Returns(new[]
            {
                new[] { "Nairobi", "Oslo", "LoneDestination" }
            });
        var agvGraphTextFixture = agvGraphTextFixtureMock.Object;

        AssertFindsShortestPath("Nairobi", "LoneDestination", agvGraphTextFixture);
    }

    private void AssertFindsShortestPath(string source, string destination, AgvGraphTestData agvGraphTextFixture)
    {
        var aStar = new AStar(agvGraphTextFixture.Graph, null, RoutingPlausibilityChecker);
        var route = aStar.CalculateRoute(source.ToGuid(), destination.ToGuid());

        route.ShouldNotBeNull();
        route.Nodes.Select(n => n.Name).ShouldBe(agvGraphTextFixture.GetAllPossibleRoutesOrdered().First());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestStaticCalculateRoute_FindsRoute_And_NoRoute(bool isEdgePlausible)
    {
        var graph = _graph!;

        var sourceNode = graph.Nodes.First(x => x.Name == "1");
        var destinationNode = graph.Nodes.First(x => x.Name == "8");

        var routingPlausibilityChecker = new Mock<IRoutingPlausibilityChecker>();
        routingPlausibilityChecker.Setup(x => x.IsPlausible(sourceNode, destinationNode)).Returns(isEdgePlausible);
        var aStar = new AStar(graph, null, routingPlausibilityChecker.Object);
        var route = aStar.CalculateRoute(sourceNode.Name.ToGuid(), destinationNode.Name.ToGuid());
        bool hasRoute = route != null;
        hasRoute.ShouldBe(isEdgePlausible);
    }

    [TestMethod]
    public void CalculateRoute_BezierGraph_CorrectResult()
    {
        var bezier = new Bezier(new List<Vector2>
        {
            new(0, 0),
            new(1, 0),
            new(1, 1)
        });

        var graph = AgvGraphGenerator.Generate(
            new List<GraphNode>
            {
                NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "01"),
                NodeGenerator.GenerateNode(x: 1, y: 1, nodeName: "02Bez"),
                NodeGenerator.GenerateNode(x: 1, y: 1, nodeName: "02Dir"),
                NodeGenerator.GenerateNode(x: 1, y: 2, nodeName: "03"),
                NodeGenerator.GenerateNode(x: 2, y: 2, nodeName: "04")
            },
            new List<GraphEdge>
            {
                GraphEdge.CreateDirect("01".ToGuid(), "02Dir".ToGuid(), (float) Math.Sqrt(2)),
                GraphEdge.CreateDirect("02Dir".ToGuid(), "03".ToGuid()),
                GraphEdge.CreateDirect("02Dir".ToGuid(), "04".ToGuid(), (float) Math.Sqrt(2)),
                GraphEdge.CreateBezier2("01".ToGuid(), "02Bez".ToGuid(), (float) bezier.GetLength(100), 1, 0),
                GraphEdge.CreateDirect("02Bez".ToGuid(), "03".ToGuid()),
                GraphEdge.CreateDirect("02Bez".ToGuid(), "04".ToGuid(), (float) Math.Sqrt(2))
            });

        var aStar = new AStar(graph, _slowTurnAgvData);

        var route = aStar.CalculateRoute("01".ToGuid(), "03".ToGuid());

        route.ShouldNotBeNull();
        route.Nodes.Length.ShouldBe(3);
        route.Nodes.Select(n => n.Name).ShouldContain("02Bez");

        var route2 = aStar.CalculateRoute("01".ToGuid(), "04".ToGuid());

        route2.ShouldNotBeNull();
        route2.Nodes.Length.ShouldBe(3);
        route2.Nodes.Select(n => n.Name).ShouldContain("02Dir");
    }

    [TestMethod]
    public void CalculateRoute_BetterNodeHasWorseHeuristic_CorrectResult()
    {
        var bezier = new Bezier(new List<Vector2>
        {
            new(0, 0),
            new(1, 0),
            new(1, 1)
        });

        var graph = AgvGraphGenerator.Generate(
            new List<GraphNode>
            {
                NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "01"),
                NodeGenerator.GenerateNode(x: 1, y: 1, nodeName: "Bez"),
                NodeGenerator.GenerateNode(x: 0.9f, y: 2, nodeName: "Dir"),
                NodeGenerator.GenerateNode(x: 1, y: 2, nodeName: "03")
            },
            new List<GraphEdge>
            {
                GraphEdge.CreateDirect("01".ToGuid(), "Dir".ToGuid(), (float) Math.Sqrt(Math.Pow(0.9, 2) + 4)),
                GraphEdge.CreateBezier2("01".ToGuid(), "Bez".ToGuid(), (float) bezier.GetLength(100), 1, 0),
                GraphEdge.CreateDirect("Bez".ToGuid(), "03".ToGuid()),
                GraphEdge.CreateDirect("Dir".ToGuid(), "03".ToGuid(), 0.1f)
            });

        var aStar = new AStar(graph, _slowTurnAgvData);

        var route = aStar.CalculateRoute("01".ToGuid(), "03".ToGuid());

        route.ShouldNotBeNull();
        route.Nodes.Length.ShouldBe(3);
        route.Nodes.Select(n => n.Name).ShouldContain("Bez");
    }

    [TestMethod]
    public void CalculateRoute_LongerPolyPathRoute_UsesDirectEdges()
    {
        /*
                Sketched graph:

                       0      1      2      3

                0      1--*
                        \ |
                          *--*
                           \ |
                1           D/P--*---3
                               \ |
                                 |
                                 | \
                2                *---4


                3

                Key:
                - "1", "P", "D", "3", "4" are nodes (representing "01", "02Poly", "02dir", "03", "04" respectively).
                - "-", "\" and "|" represent edge connections.
                - "*" represents a control point in a polypath edge.
       */

        var nodes = new List<GraphNode>
        {
            NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "01"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeName: "02Poly"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeName: "02Dir"),
            NodeGenerator.GenerateNode(x: 1, y: 2, nodeName: "03"),
            NodeGenerator.GenerateNode(x: 2, y: 2, nodeName: "04")
        };
        var graph = AgvGraphGenerator.Generate(
            nodes,
            [
                GraphEdge.CreateDirect("01".ToGuid(), "02Dir".ToGuid(), (float) Math.Sqrt(2)),
                GraphEdge.CreateDirect("02Dir".ToGuid(), "03".ToGuid()),
                GraphEdge.CreateDirect("02Dir".ToGuid(), "04".ToGuid(), (float) Math.Sqrt(2)),
                AgvGraphGenerator.CreatePolypathEdge("01",
                    "02Poly",
                    [
                        new EdgeCoefficient(0.5, 0),
                        new EdgeCoefficient(0.5, 0.5),
                        new EdgeCoefficient(1, 0.5)
                    ],
                    [.. nodes]),
                GraphEdge.CreateDirect("02Poly".ToGuid(), "03".ToGuid()),
                AgvGraphGenerator.CreatePolypathEdge("02Poly",
                    "04",
                    [
                        new EdgeCoefficient(1.5, 1),
                        new EdgeCoefficient(1.5, 2)
                    ],
                    [.. nodes])
            ]);

        var aStar = new AStar(graph, _slowTurnAgvData);

        var route = aStar.CalculateRoute("01".ToGuid(), "03".ToGuid());

        route.ShouldNotBeNull();
        route.Nodes.Length.ShouldBe(3);
        route.Nodes.Select(n => n.Name).ShouldContain("02Dir");

        var route2 = aStar.CalculateRoute("01".ToGuid(), "04".ToGuid());

        route2.ShouldNotBeNull();
        route2.Nodes.Length.ShouldBe(3);
        route2.Nodes.Select(n => n.Name).ShouldContain("02Dir");
    }

    [TestMethod]
    public void CalculateRoute_ShorterPolyPathRoute_UsesPolyPath()
    {
        /*
                Sketched graph:

                       0      1      2      3

                0      1 -----5
                        \     |
                          *   |
                           \  |
                1           D/P------3
                               \     |
                                 *   |
                                   \ |
                2                    4


                3

                Key:
                - "1", "P", "D", "3", "4", "5" are nodes (representing "01", "02Poly", "02dir", "03", "04", "05" respectively).
                - "-", "\" and "|" represent edge connections.
                - "*" represents a control point in a polypath edge.
       */

        var nodes = new List<GraphNode>
        {
            NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "01"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeName: "02Poly"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeName: "02Dir"),
            NodeGenerator.GenerateNode(x: 1, y: 2, nodeName: "03"),
            NodeGenerator.GenerateNode(x: 2, y: 2, nodeName: "04"),
            NodeGenerator.GenerateNode(x: 1, y: 0, nodeName: "05")
        };
        var graph = AgvGraphGenerator.Generate(
            nodes,
            [
                GraphEdge.CreateDirect("01".ToGuid(), "05".ToGuid()),
                GraphEdge.CreateDirect("02Dir".ToGuid(), "03".ToGuid()),
                GraphEdge.CreateDirect("02Poly".ToGuid(), "03".ToGuid()),
                GraphEdge.CreateDirect("03".ToGuid(), "04".ToGuid()),
                GraphEdge.CreateDirect("05".ToGuid(), "02Dir".ToGuid()),
                AgvGraphGenerator.CreatePolypathEdge("01",
                    "02Poly",
                    [
                        new EdgeCoefficient(0.5, 0.5)
                    ],
                    [.. nodes]),
                AgvGraphGenerator.CreatePolypathEdge("02Poly",
                    "04",
                    [
                        new EdgeCoefficient(1.5, 1.5)
                    ],
                    [.. nodes])
            ]);

        var aStar = new AStar(graph, _slowTurnAgvData);

        var route = aStar.CalculateRoute("01".ToGuid(), "03".ToGuid());

        route.ShouldNotBeNull();
        route.Nodes.Length.ShouldBe(3);
        route.Nodes.Select(n => n.Name).ShouldContain("02Poly");

        var route2 = aStar.CalculateRoute("01".ToGuid(), "04".ToGuid());

        route2.ShouldNotBeNull();
        route2.Nodes.Length.ShouldBe(3);
        route2.Nodes.Select(n => n.Name).ShouldContain("02Poly");
    }

    [TestMethod]
    public void CalculateRoute_LongerSlantedRoute_UsesDirectEdges()
    {
        /*
                Sketched graph:

                       0      1      2      3

                0      1----*
                        \  /
                          \
                         /  \
                1       *----D/S---*-3
                               \  /
                                 \
                                /  \
                2              *-----4


                3

                Key:
                - "1", "S", "D", "3", "4" are nodes (representing "01", "02Slant", "02dir", "03", "04" respectively).
                - "-", "\" and "|" represent edge connections.
                - "*" the location of the exit distance in a slanted edge.
       */
        const float west = 3f * (float) Math.PI / 2f;
        var nodes = new List<GraphNode>
        {
            NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "01"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeName: "02Slant"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeName: "02Dir"),
            NodeGenerator.GenerateNode(x: 1, y: 2, nodeName: "03"),
            NodeGenerator.GenerateNode(x: 2, y: 2, nodeName: "04")
        };
        var graph = AgvGraphGenerator.Generate(
            nodes,
            [
                GraphEdge.CreateDirect("01".ToGuid(), "02Dir".ToGuid(), (float) Math.Sqrt(2), "01-02Dir".ToGuid()),
                GraphEdge.CreateDirect("02Dir".ToGuid(), "03".ToGuid(), id: "02Dir-03".ToGuid()),
                GraphEdge.CreateDirect("02Dir".ToGuid(), "04".ToGuid(), (float) Math.Sqrt(2), id: "02Dir-04".ToGuid()),
                AgvGraphGenerator.CreateSlantedEdge("01", "02Slant", west, 0.9, [.. nodes]),
                GraphEdge.CreateDirect("02Slant".ToGuid(), "03".ToGuid(), id: "02Slant-03".ToGuid()),
                AgvGraphGenerator.CreateSlantedEdge("02Slant", "04", west, 0.9, [.. nodes])
            ]);

        var aStar = new AStar(graph, _slowTurnAgvData);

        var route = aStar.CalculateRoute("01".ToGuid(), "03".ToGuid());

        route.ShouldNotBeNull();
        route.Nodes.Length.ShouldBe(3);
        route.Nodes.Select(n => n.Name).ShouldContain("02Dir");
        route.Edges.ShouldBe(["01-02Dir".ToGuid(), "02Dir-03".ToGuid()]);

        var route2 = aStar.CalculateRoute("01".ToGuid(), "04".ToGuid());

        route2.ShouldNotBeNull();
        route2.Nodes.Length.ShouldBe(3);
        route2.Nodes.Select(n => n.Name).ShouldContain("02Dir");
        route2.Edges.ShouldBe(["01-02Dir".ToGuid(), "02Dir-04".ToGuid()]);
    }

    [TestMethod]
    public void CalculateRoute_ShorterSlantedRoute_UsesSlanted()
    {
        /*
                Sketched graph:

                       0      1      2      3

                0      1 *----5
                          \   |
                           \  |
                           \  |
                1           *D/S*----3
                                 \   |
                                  \  |
                                   \ |
                2                   *4


                3

                Key:
                - "1", "S", "D", "3", "4", "5" are nodes (representing "01", "02Slant", "02dir", "03", "04", "05" respectively).
                - "-", "\" and "|" represent edge connections.
                - "*" represents the location of the exit distance in a slanted edge.
       */
        const float west = 3f * (float) Math.PI / 2f;
        var nodes = new List<GraphNode>
        {
            NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "01"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeName: "02Slant"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeName: "02Dir"),
            NodeGenerator.GenerateNode(x: 1, y: 2, nodeName: "03"),
            NodeGenerator.GenerateNode(x: 2, y: 2, nodeName: "04"),
            NodeGenerator.GenerateNode(x: 1, y: 0, nodeName: "05")
        };
        var graph = AgvGraphGenerator.Generate(
            nodes,
            [
                GraphEdge.CreateDirect("01".ToGuid(), "05".ToGuid(), id: "01-05".ToGuid()),
                GraphEdge.CreateDirect("02Dir".ToGuid(), "03".ToGuid(), id: "02Dir-03".ToGuid()),
                GraphEdge.CreateDirect("02Slant".ToGuid(), "03".ToGuid(), id: "02Slant-03".ToGuid()),
                GraphEdge.CreateDirect("03".ToGuid(), "04".ToGuid(), id: "03-04".ToGuid()),
                GraphEdge.CreateDirect("05".ToGuid(), "02Dir".ToGuid(), id: "05-02Dir".ToGuid()),
                AgvGraphGenerator.CreateSlantedEdge("01", "02Slant", west, 0.0, [.. nodes]),
                AgvGraphGenerator.CreateSlantedEdge("02Slant", "04", west, 0.0, [.. nodes])
            ]);

        var id0102Slant = graph.Edges.Single(e => e.Variant == EdgeType.Slanted && e.ToNodeId == "02Slant".ToGuid()).Id;
        var id02Slant04 = graph.Edges.Single(e => e.Variant == EdgeType.Slanted && e.ToNodeId == "04".ToGuid()).Id;

        var aStar = new AStar(graph, _slowTurnAgvData);

        var route = aStar.CalculateRoute("01".ToGuid(), "03".ToGuid());

        route.ShouldNotBeNull();
        route.Nodes.Length.ShouldBe(3);
        route.Nodes.Select(n => n.Name).ShouldContain("02Slant");
        route.Edges.ShouldBe([id0102Slant, "02Slant-03".ToGuid()]);

        var route2 = aStar.CalculateRoute("01".ToGuid(), "04".ToGuid());

        route2.ShouldNotBeNull();
        route2.Nodes.Length.ShouldBe(3);
        route2.Nodes.Select(n => n.Name).ShouldContain("02Slant");
        route2.Edges.ShouldBe([id0102Slant, id02Slant04]);
    }

    [TestMethod]
    public void CalculateRoute_TwoEdgesWithSameSourceTarget_UsesDirectEdges()
    {
        /*
                Sketched graph:

                       0      1      2      3

                0      1--*
                        \ |
                          *--*
                           \ |
                1            2--*---3
                               \ |
                                 |
                                 | \
                2                *---4


                3

                Key:
                - "1", "P", "D", "3", "4" are nodes (representing "01", "02", "03", "04" respectively).
                - "-", "\" and "|" represent edge connections.
                - "*" represents a control point in a polypath edge.
       */

        var nodes = new List<GraphNode>
        {
            NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "01"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeName: "02"),
            NodeGenerator.GenerateNode(x: 1, y: 2, nodeName: "03"),
            NodeGenerator.GenerateNode(x: 2, y: 2, nodeName: "04")
        };

        var directEdge0102 = GraphEdge.CreateDirect("01".ToGuid(), "02".ToGuid(), (float) Math.Sqrt(2));
        var directEdge0204 = GraphEdge.CreateDirect("02".ToGuid(), "04".ToGuid(), (float) Math.Sqrt(2));
        var polyPathEdge0102 = AgvGraphGenerator.CreatePolypathEdge("01",
            "02",
            [
                new EdgeCoefficient(0.5, 0),
                new EdgeCoefficient(0.5, 0.5),
                new EdgeCoefficient(1, 0.5)
            ],
            [.. nodes]);
        var polyPathEdge0204 = AgvGraphGenerator.CreatePolypathEdge("02",
            "04",
            [
                new EdgeCoefficient(1.5, 1),
                new EdgeCoefficient(1.5, 2)
            ],
            [.. nodes]);

        var graph = AgvGraphGenerator.Generate(
            nodes,
            [
                directEdge0102,
                GraphEdge.CreateDirect("02".ToGuid(), "03".ToGuid()),
                directEdge0204,
                polyPathEdge0102,
                polyPathEdge0204
            ]);

        var aStar = new AStar(graph, _slowTurnAgvData);

        var route = aStar.CalculateRoute("01".ToGuid(), "03".ToGuid());

        route.ShouldNotBeNull();
        route.Nodes.Length.ShouldBe(3);
        route.Nodes.Select(n => n.Name).ShouldContain("02");
        route.Edges.ShouldContain(directEdge0102.Id);

        var route2 = aStar.CalculateRoute("01".ToGuid(), "04".ToGuid());

        route2.ShouldNotBeNull();
        route2.Nodes.Length.ShouldBe(3);
        route2.Nodes.Select(n => n.Name).ShouldContain("02");
        route2.Edges.ShouldContain(directEdge0204.Id);
    }
}