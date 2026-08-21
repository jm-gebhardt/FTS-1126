using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Logging;
using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.TrafficManagement;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Providers;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Reservations;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Generation;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Manager;

namespace Gebhardt.Acc.TransportOrder.Test.TrafficManagement.Routes.Manager;

/// <summary>
/// Provides a RouteManager with real RouteGenerator and ReservationManager instances and a MassTransit test harness.
/// </summary>
internal sealed class RouteManagerTestFixture : MassTransitTestFixture
{
    private static readonly AgvType _agvType = new(Guid.NewGuid(), "TestAgvType");

    public ReservationManager ReservationManager { get; }

    public RouteManager RouteManager { get; }

    public RouteManagerTestFixture()
    {
        Build();

        TrafficManagementConfig.MaxReservationLength = 8; // fix MaxReservationLength for the tests.

        ReservationManager = new ReservationManager(new ConsoleLogger<ReservationManager>());
        RouteManager = new RouteManager(
            new RouteGenerator(new ConsoleLogger<RouteGenerator>()),
            ReservationManager,
            TestHarness.Bus,
            new ConsoleLogger<RouteManager>());
    }

    /// <summary>
    /// Creates a linear chain of routing nodes connected by routing edges of the given length.
    /// </summary>
    public static (RoutingNode[] Nodes, RoutingEdge[] Edges) CreateChain(int edgeCount, double edgeLength)
    {
        var nodes = new RoutingNode[edgeCount + 1];
        var edges = new RoutingEdge[edgeCount];

        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i] = RoutingResourceGenerator.CreateRoutingNode(_agvType, i * edgeLength);
        }

        for (int i = 0; i < edges.Length; i++)
        {
            edges[i] = RoutingResourceGenerator.CreateRoutingEdge(nodes[i], nodes[i + 1], edgeLength);
        }

        return (nodes, edges);
    }

    public static RoutingNode CreateIsolatedNode(double x = 0)
    {
        return RoutingResourceGenerator.CreateRoutingNode(_agvType, x);
    }
}

/// <summary>
/// Configurable AGV stub for route manager tests.
/// </summary>
internal sealed class RoutingAgvStub : IRoutingAgv
{
    public Guid CorrelationId { get; } = Guid.NewGuid();

    public RoutingNode? CurrentRoutingNode { get; init; }

    public bool IsInAutomaticMode { get; init; } = true;
}
