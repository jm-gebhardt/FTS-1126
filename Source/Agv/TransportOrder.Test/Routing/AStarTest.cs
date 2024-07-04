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
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        _graph = new AgvGraph(Guid.Empty,
            new[]
            {
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 0f, 5f, nodeId: "1"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 1f, 5f, nodeId: "2"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, nodeId: "3"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 3f, 3f, nodeId: "4"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 3f, 6f, nodeId: "5"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 3f, nodeId: "6"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 6f, 0f, nodeId: "7"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 7f, 1f, nodeId: "8"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 8f, nodeId: "9"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 8f, 5f, nodeId: "10")
            },
            new[]
            {
                GraphEdge.CreateDirect("1", "2"),
                GraphEdge.CreateDirect("1", "4", 4f),
                GraphEdge.CreateDirect("4", "1", 4f),
                GraphEdge.CreateDirect("2", "5", 3f),
                GraphEdge.CreateDirect("3", "5", 5f),
                GraphEdge.CreateDirect("3", "4", 3f),
                GraphEdge.CreateDirect("4", "3", 3f),
                GraphEdge.CreateDirect("3", "6", 2f),
                GraphEdge.CreateDirect("6", "3", 2f),
                GraphEdge.CreateDirect("5", "8", 7f),
                GraphEdge.CreateDirect("8", "5", 7f),
                GraphEdge.CreateDirect("8", "7"),
                GraphEdge.CreateDirect("7", "8"),
                GraphEdge.CreateDirect("9", "8"),
                GraphEdge.CreateDirect("7", "10", 6f)
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
                    var next = NodeGenerator.GenerateNode(AgvGraph.NodeType.WayPoint, x, y, nodeId: $"{x:D2}/{y:D2}");
                    nodes.Add(next);
                    var prev = nodes.SingleOrDefault(n => n.Id == $"{x - 1:D2}/{y:D2}");
                    if (prev != null)
                    {
                        edges.Add(GraphEdge.CreateDirect(prev.Id, next.Id));
                        edges.Add(GraphEdge.CreateDirect(next.Id, prev.Id));
                    }
                    prev = nodes.SingleOrDefault(n => n.Id == $"{x:D2}/{y - 1:D2}");
                    if (prev != null)
                    {
                        edges.Add(GraphEdge.CreateDirect(prev.Id, next.Id));
                        edges.Add(GraphEdge.CreateDirect(next.Id, prev.Id));
                    }
                }
                ++x;
            }
            ++y;
        }
        _grid = new AgvGraph(Guid.Empty, nodes.ToArray(), edges.ToArray());

        _wikiExample = new AgvGraph(Guid.Empty,
            new[]
            {
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 0f, 0f, nodeId: "Nairobi"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 42f, 80f, nodeId: "Tokyo"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 104f, 100f, nodeId: "Marseille"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 96f, 60f, nodeId: "Berlin"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 104f, -10f, nodeId: "Denver"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 150f, 20f, nodeId: "Helsinki"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 200f, 92f, nodeId: "Rio")
            },
            new[]
            {
                GraphEdge.CreateDirect("Nairobi", "Tokyo", 70f),
                GraphEdge.CreateDirect("Nairobi", "Denver", 145f),
                GraphEdge.CreateDirect("Denver", "Nairobi", 145f),
                GraphEdge.CreateDirect("Tokyo", "Marseille", 103f),
                GraphEdge.CreateDirect("Tokyo", "Berlin", 53f),
                GraphEdge.CreateDirect("Denver", "Helsinki", 84f),
                GraphEdge.CreateDirect("Marseille", "Rio", 116f),
                GraphEdge.CreateDirect("Berlin", "Rio", 183f),
                GraphEdge.CreateDirect("Helsinki", "Rio", 102f)
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
            new(9f, Array.Empty<string>(), Array.Empty<Guid>()),
            new(1f, Array.Empty<string>(), Array.Empty<Guid>()),
            null,
            new(5f, Array.Empty<string>(), Array.Empty<Guid>())
        };

        List<Route?> sorted = routes.OrderByDescending(n => n != null).ThenBy(n => n).ToList();
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

        var route = aStar.CalculateRoute("Nairobi", "Rio");
        route.ShouldNotBeNull();
        route.Nodes.ShouldBe(new[] { "Nairobi", "Tokyo", "Marseille", "Rio" });
        route.Costs.ShouldBe(291.8725786, 0.00001f);
    }

    [TestMethod]
    public void Test_AStar_WikipediaExample_SameSourceDestination()
    {
        var aStar = new AStar(_wikiExample, null, RoutingPlausibilityChecker);

        var route = aStar.CalculateRoute("Nairobi", "Nairobi");
        route.ShouldNotBeNull();
        route.Nodes.ShouldBe(new[] { "Nairobi" });
        route.Costs.ShouldBe(0f, 0.00001f);
    }

    [TestMethod]
    public void Test_AStar_WikipediaExample_WithCurrentNode()
    {
        var aStar = new AStar(_wikiExample, _agvData, RoutingPlausibilityChecker);

        var route = aStar.CalculateRoute("Denver", "Nairobi", "Rio");
        route.ShouldNotBeNull();
        route.Nodes.ShouldBe(new[] { "Denver", "Nairobi", "Tokyo", "Marseille", "Rio" });
        route.Costs.ShouldBe(289 + 145 + 3.3725786, 0.00001f);
    }

    [TestMethod]
    public void Test_AStar_WikipediaExample_MultipleWaypoints()
    {
        var aStar = new AStar(_wikiExample, _agvData, RoutingPlausibilityChecker);

        string[] waypoints = { "Denver", "Nairobi", "Nairobi", "Rio" }; // (Denver => Nairobi) + (Nairobi => Rio)

        var route = aStar.CalculateRoute(waypoints);
        route.ShouldNotBeNull();
        route.Nodes.ShouldBe(new[] { "Denver", "Nairobi", "Tokyo", "Marseille", "Rio" });
        route.Costs.ShouldBe(289 + 145 + 3.3725786, 0.00001f);
    }

    /// <summary>
    /// Test AStar implementation with am example having bidirectional edges.
    /// </summary>
    [TestMethod]
    public void Test_AStarBidirectional_UseGraphSeveralTimes()
    {
        var aStar = new AStar(_graph!, _agvData, RoutingPlausibilityChecker);

        var route = aStar.CalculateRoute("1", "10");
        route.ShouldNotBeNull();
        route.Costs.ShouldBe(18f + 6.416286f, 0.00001f);

        route = aStar.CalculateRoute("10", "1");
        route.ShouldBeNull();

        route = aStar.CalculateRoute("9", "5");
        route.ShouldNotBeNull();
        route.Costs.ShouldBe(8 + 2.03522328, 0.00001f);

        route = aStar.CalculateRoute("4", "9");
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

        c1 = aStar.CalculateRoute("01/01", "11/01");
        c1!.Costs.ShouldBe(10.5f, 0.00001f);
        c2 = aStar.CalculateRoute("11/01", "11/02");
        c2!.Costs.ShouldBe(1.5f, 0.00001f);
        c3 = aStar.CalculateRoute("11/02", "50/02");
        c3!.Costs.ShouldBe(39.5f, 0.00001f);
        c4 = aStar.CalculateRoute("50/02", "50/01");
        c4!.Costs.ShouldBe(1.5f, 0.00001f);

        costs = aStar.CalculateRoute("01/01", "50/01");
        costs!.Costs.ShouldBe(c1.Costs + c2.Costs + c3.Costs + c4.Costs + 3, 0.00001f);

        c1 = aStar.CalculateRoute("50/01", "13/17");
        c1!.Costs.ShouldBe(60.5f, 0.00001f);
        c2 = aStar.CalculateRoute("13/17", "33/17");
        c2!.Costs.ShouldBe(10f * 13f + 29f, 0.00001f);
        c3 = aStar.CalculateRoute("33/17", "33/06");
        c3!.Costs.ShouldBe(11.5f, 0.00001f);

        var stopWatch = new Stopwatch();
        stopWatch.Start();
        costs = aStar.CalculateRoute("50/01", "33/06");
        stopWatch.Stop();
        Trace.WriteLine("Grid example duration: " + stopWatch.ElapsedMilliseconds + "ms.");
        costs!.Costs.ShouldBe(c1.Costs + c2.Costs + c3.Costs + 0.5, 0.00001f);
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
        var aStar = new AStar(_grid!, null, RoutingPlausibilityChecker);
        aStar.CalculateRoute(source, destination).ShouldBeNull();
    }

    /// <summary>
    /// Test path to unreachable node.
    /// </summary>
    [TestMethod]
    public void Test_AStarGrid_UnreachableNode_NoPath()
    {
        var aStar = new AStar(_grid!, null, RoutingPlausibilityChecker);
        var route = aStar.CalculateRoute("01/01", "47/23");
        route.ShouldBeNull();
    }

    [TestMethod]
    public void CalculateRoute_ReturnsNull_WhenRouteNotPlausible()
    {
        var graph = new AgvGraph(Guid.Empty,
            new[]
            {
                NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 0f, 5f, nodeId: "1"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.Drop, 1f, 5f, nodeId: "2")
            },
            new[]
            {
                GraphEdge.CreateDirect("1", "2"),
                GraphEdge.CreateDirect("2", "1", 4f)
            });

        var routingPlausibilityCheckerMock = new Mock<IRoutingPlausibilityChecker>();
        routingPlausibilityCheckerMock.Setup(r => r.IsPlausible(It.IsAny<GraphNode>(), It.IsAny<GraphNode>())).Returns(true);

        var aStar = new AStar(graph, null, routingPlausibilityCheckerMock.Object);
        var route = aStar.CalculateRoute("1", "2");
        route.ShouldNotBeNull();

        routingPlausibilityCheckerMock.Setup(r => r.IsPlausible(It.IsAny<GraphNode>(), It.IsAny<GraphNode>())).Returns(false);
        aStar = new AStar(graph, null, routingPlausibilityCheckerMock.Object);
        route = aStar.CalculateRoute("1", "2");
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
        var route = aStar.CalculateRoute(source, destination);

        route.ShouldNotBeNull();
        route.Nodes.ShouldBe(agvGraphTextFixture.GetAllPossibleRoutesOrdered().First());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestStaticCalculateRoute_FindsRoute_And_NoRoute(bool isEdgePlausible)
    {
        var graph = _graph!;

        var sourceNode = graph.Nodes.First(x => x.Id == "1");
        var destinationNode = graph.Nodes.First(x => x.Id == "8");

        var routingPlausibilityChecker = new Mock<IRoutingPlausibilityChecker>();
        routingPlausibilityChecker.Setup(x => x.IsPlausible(sourceNode, destinationNode)).Returns(isEdgePlausible);
        var aStar = new AStar(graph, null, routingPlausibilityChecker.Object);
        var route = aStar.CalculateRoute(sourceNode.Id, destinationNode.Id);
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

        var graph = new AgvGraph(
            Guid.NewGuid(),
            new List<GraphNode>
            {
                NodeGenerator.GenerateNode(x: 0, y: 0, nodeId: "01"),
                NodeGenerator.GenerateNode(x: 1, y: 1, nodeId: "02Bez"),
                NodeGenerator.GenerateNode(x: 1, y: 1, nodeId: "02Dir"),
                NodeGenerator.GenerateNode(x: 1, y: 2, nodeId: "03"),
                NodeGenerator.GenerateNode(x: 2, y: 2, nodeId: "04")
            },
            new List<GraphEdge>
            {
                GraphEdge.CreateDirect("01", "02Dir", (float)Math.Sqrt(2)),
                GraphEdge.CreateDirect("02Dir", "03"),
                GraphEdge.CreateDirect("02Dir", "04", (float)Math.Sqrt(2)),
                GraphEdge.CreateBezier1("01", "02Bez", (float)bezier.GetLength(100), 1, 0),
                GraphEdge.CreateDirect("02Bez", "03"),
                GraphEdge.CreateDirect("02Bez", "04", (float)Math.Sqrt(2))
            }
        );

        var aStar = new AStar(graph, _slowTurnAgvData);

        var route = aStar.CalculateRoute("01", "03");

        route.ShouldNotBeNull();
        route.Nodes.Length.ShouldBe(3);
        route.Nodes.ShouldContain("02Bez");

        var route2 = aStar.CalculateRoute("01", "04");

        route2.ShouldNotBeNull();
        route2.Nodes.Length.ShouldBe(3);
        route2.Nodes.ShouldContain("02Dir");
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

        var graph = new AgvGraph(
            Guid.NewGuid(),
            new List<GraphNode>
            {
                NodeGenerator.GenerateNode(x: 0, y: 0, nodeId: "01"),
                NodeGenerator.GenerateNode(x: 1, y: 1, nodeId: "Bez"),
                NodeGenerator.GenerateNode(x: 0.9f, y: 2, nodeId: "Dir"),
                NodeGenerator.GenerateNode(x: 1, y: 2, nodeId: "03")
            },
            new List<GraphEdge>
            {
                GraphEdge.CreateDirect("01", "Dir", (float)Math.Sqrt(Math.Pow(0.9, 2) + 4)),
                GraphEdge.CreateBezier1("01", "Bez", (float)bezier.GetLength(100), 1, 0),
                GraphEdge.CreateDirect("Bez", "03"),
                GraphEdge.CreateDirect("Dir", "03", 0.1f)
            }
        );

        var aStar = new AStar(graph, _slowTurnAgvData);

        var route = aStar.CalculateRoute("01", "03");

        route.ShouldNotBeNull();
        route.Nodes.Length.ShouldBe(3);
        route.Nodes.ShouldContain("Bez");
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
            NodeGenerator.GenerateNode(x: 0, y: 0, nodeId: "01"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeId: "02Poly"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeId: "02Dir"),
            NodeGenerator.GenerateNode(x: 1, y: 2, nodeId: "03"),
            NodeGenerator.GenerateNode(x: 2, y: 2, nodeId: "04")
        };
        var graph = new AgvGraph(
            Guid.NewGuid(),
            nodes,
            [
                GraphEdge.CreateDirect("01", "02Dir", (float)Math.Sqrt(2)),
                GraphEdge.CreateDirect("02Dir", "03"),
                GraphEdge.CreateDirect("02Dir", "04", (float)Math.Sqrt(2)),
                AgvGraphGenerator.CreatePolypathEdge("01",
                    "02Poly",
                    [
                        new EdgeCoefficient(0.5, 0),
                        new EdgeCoefficient(0.5, 0.5),
                        new EdgeCoefficient(1, 0.5)
                    ],
                    [.. nodes]),
                GraphEdge.CreateDirect("02Poly", "03"),
                AgvGraphGenerator.CreatePolypathEdge("02Poly",
                    "04",
                    [
                        new EdgeCoefficient(1.5, 1),
                        new EdgeCoefficient(1.5, 2)
                    ],
                    [.. nodes])
            ]
        );

        var aStar = new AStar(graph, _slowTurnAgvData);

        var route = aStar.CalculateRoute("01", "03");

        route.ShouldNotBeNull();
        route.Nodes.Length.ShouldBe(3);
        route.Nodes.ShouldContain("02Dir");

        var route2 = aStar.CalculateRoute("01", "04");

        route2.ShouldNotBeNull();
        route2.Nodes.Length.ShouldBe(3);
        route2.Nodes.ShouldContain("02Dir");
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
            NodeGenerator.GenerateNode(x: 0, y: 0, nodeId: "01"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeId: "02Poly"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeId: "02Dir"),
            NodeGenerator.GenerateNode(x: 1, y: 2, nodeId: "03"),
            NodeGenerator.GenerateNode(x: 2, y: 2, nodeId: "04"),
            NodeGenerator.GenerateNode(x: 1, y: 0, nodeId: "05")
        };
        var graph = new AgvGraph(
            Guid.NewGuid(),
            nodes,
            [
                GraphEdge.CreateDirect("01", "05"),
                GraphEdge.CreateDirect("02Dir", "03"),
                GraphEdge.CreateDirect("02Poly", "03"),
                GraphEdge.CreateDirect("03", "04"),
                GraphEdge.CreateDirect("05", "02Dir"),
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
            ]
        );

        var aStar = new AStar(graph, _slowTurnAgvData);

        var route = aStar.CalculateRoute("01", "03");

        route.ShouldNotBeNull();
        route.Nodes.Length.ShouldBe(3);
        route.Nodes.ShouldContain("02Poly");

        var route2 = aStar.CalculateRoute("01", "04");

        route2.ShouldNotBeNull();
        route2.Nodes.Length.ShouldBe(3);
        route2.Nodes.ShouldContain("02Poly");
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
        const float west = 3f * (float)Math.PI / 2f;
        var nodes = new List<GraphNode>
        {
            NodeGenerator.GenerateNode(x: 0, y: 0, nodeId: "01"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeId: "02Slant"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeId: "02Dir"),
            NodeGenerator.GenerateNode(x: 1, y: 2, nodeId: "03"),
            NodeGenerator.GenerateNode(x: 2, y: 2, nodeId: "04")
        };
        var graph = new AgvGraph(
            Guid.NewGuid(),
            nodes,
            [
                GraphEdge.CreateDirect("01", "02Dir", (float)Math.Sqrt(2), "01-02Dir".ToGuid()),
                GraphEdge.CreateDirect("02Dir", "03", id: "02Dir-03".ToGuid()),
                GraphEdge.CreateDirect("02Dir", "04", (float)Math.Sqrt(2), id: "02Dir-04".ToGuid()),
                AgvGraphGenerator.CreateSlantedEdge("01", "02Slant", west, 0.9, [.. nodes]),
                GraphEdge.CreateDirect("02Slant", "03", id: "02Slant-03".ToGuid()),
                AgvGraphGenerator.CreateSlantedEdge("02Slant", "04", west, 0.9, [.. nodes])
            ]
        );

        var aStar = new AStar(graph, _slowTurnAgvData);

        var route = aStar.CalculateRoute("01", "03");

        route.ShouldNotBeNull();
        route.Nodes.Length.ShouldBe(3);
        route.Nodes.ShouldContain("02Dir");
        route.Edges.ShouldBe(["01-02Dir".ToGuid(), "02Dir-03".ToGuid()]);

        var route2 = aStar.CalculateRoute("01", "04");

        route2.ShouldNotBeNull();
        route2.Nodes.Length.ShouldBe(3);
        route2.Nodes.ShouldContain("02Dir");
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
        const float west = 3f * (float)Math.PI / 2f;
        var nodes = new List<GraphNode>
        {
            NodeGenerator.GenerateNode(x: 0, y: 0, nodeId: "01"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeId: "02Slant"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeId: "02Dir"),
            NodeGenerator.GenerateNode(x: 1, y: 2, nodeId: "03"),
            NodeGenerator.GenerateNode(x: 2, y: 2, nodeId: "04"),
            NodeGenerator.GenerateNode(x: 1, y: 0, nodeId: "05")
        };
        var graph = new AgvGraph(
            Guid.NewGuid(),
            nodes,
            [
                GraphEdge.CreateDirect("01", "05", id: "01-05".ToGuid()),
                GraphEdge.CreateDirect("02Dir", "03", id: "02Dir-03".ToGuid()),
                GraphEdge.CreateDirect("02Slant", "03", id: "02Slant-03".ToGuid()),
                GraphEdge.CreateDirect("03", "04", id: "03-04".ToGuid()),
                GraphEdge.CreateDirect("05", "02Dir", id: "05-02Dir".ToGuid()),
                AgvGraphGenerator.CreateSlantedEdge("01", "02Slant", west, 0.0, [.. nodes]),
                AgvGraphGenerator.CreateSlantedEdge("02Slant", "04", west, 0.0, [.. nodes])
            ]
        );

        var id0102Slant = graph.Edges.Single(e => e is { Variant: EdgeType.Slanted, ToNodeId: "02Slant" }).Id;
        var id02Slant04 = graph.Edges.Single(e => e is { Variant: EdgeType.Slanted, ToNodeId: "04" }).Id;

        var aStar = new AStar(graph, _slowTurnAgvData);

        var route = aStar.CalculateRoute("01", "03");

        route.ShouldNotBeNull();
        route.Nodes.Length.ShouldBe(3);
        route.Nodes.ShouldContain("02Slant");
        route.Edges.ShouldBe([id0102Slant, "02Slant-03".ToGuid()]);

        var route2 = aStar.CalculateRoute("01", "04");

        route2.ShouldNotBeNull();
        route2.Nodes.Length.ShouldBe(3);
        route2.Nodes.ShouldContain("02Slant");
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
            NodeGenerator.GenerateNode(x: 0, y: 0, nodeId: "01"),
            NodeGenerator.GenerateNode(x: 1, y: 1, nodeId: "02"),
            NodeGenerator.GenerateNode(x: 1, y: 2, nodeId: "03"),
            NodeGenerator.GenerateNode(x: 2, y: 2, nodeId: "04")
        };

        var directEdge0102 = GraphEdge.CreateDirect("01", "02", (float)Math.Sqrt(2));
        var directEdge0204 = GraphEdge.CreateDirect("02", "04", (float)Math.Sqrt(2));
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
        
        var graph = new AgvGraph(
            Guid.NewGuid(),
            nodes,
            [
                directEdge0102,
                GraphEdge.CreateDirect("02", "03"),
                directEdge0204,
                polyPathEdge0102,
                polyPathEdge0204
            ]
        );

        var aStar = new AStar(graph, _slowTurnAgvData);

        var route = aStar.CalculateRoute("01", "03");

        route.ShouldNotBeNull();
        route.Nodes.Length.ShouldBe(3);
        route.Nodes.ShouldContain("02");
        route.Edges.ShouldContain(directEdge0102.Id);

        var route2 = aStar.CalculateRoute("01", "04");

        route2.ShouldNotBeNull();
        route2.Nodes.Length.ShouldBe(3);
        route2.Nodes.ShouldContain("02");
        route2.Edges.ShouldContain(directEdge0204.Id);
    }
}