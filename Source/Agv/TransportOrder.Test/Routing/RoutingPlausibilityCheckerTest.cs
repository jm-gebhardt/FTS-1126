using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Routing.Base;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Routing;

[TestClass]
public class RoutingPlausibilityCheckerTest
{
    private IRoutingPlausibilityChecker _routingPlausibilityChecker = null!;

    [TestInitialize]
    public void Setup()
    {
        _routingPlausibilityChecker = new RoutingPlausibilityChecker();
    }

    [TestMethod]
    [DynamicData(nameof(NoneIsActionType), DynamicDataSourceType.Method)]
    public void IsPlausible_ReturnsTrue_WhenNoneIsOfActionType(GraphNode source, GraphNode destination)
    {
        bool isPlausible = _routingPlausibilityChecker.IsPlausible(source, destination);
        isPlausible.ShouldBeTrue();
    }

    [TestMethod]
    [DynamicData(nameof(SourcesGoesToDestination), DynamicDataSourceType.Method)]
    public void IsPlausible_ReturnsTrue_WhenSourceGoesToDestination(GraphNode source, GraphNode destination)
    {
        bool isPlausible = _routingPlausibilityChecker.IsPlausible(source, destination);
        isPlausible.ShouldBeTrue();
    }

    [TestMethod]
    [DynamicData(nameof(SourcesGoesNotToDestination), DynamicDataSourceType.Method)]
    [DynamicData(nameof(NoSourcesGoesToDestination), DynamicDataSourceType.Method)]
    public void IsPlausible_ReturnsFalse_WhenSourceGoesNotToDestination(GraphNode source, GraphNode destination)
    {
        bool isPlausible = _routingPlausibilityChecker.IsPlausible(source, destination);
        isPlausible.ShouldBeFalse();
    }

    [TestMethod]
    [DynamicData(nameof(OnlyOneIsActionType), DynamicDataSourceType.Method)]
    public void IsPlausible_ReturnsFalse_WhenSourceGoesToDestination(GraphNode source, GraphNode destination)
    {
        bool isPlausible = _routingPlausibilityChecker.IsPlausible(source, destination);
        isPlausible.ShouldBeFalse();
    }

    private static IEnumerable<object[]> SourcesGoesNotToDestination()
    {
        yield return new object[] { CreateNode(AgvGraph.NodeType.Pick), CreateNode(AgvGraph.NodeType.WayPoint) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Pick), CreateNode(AgvGraph.NodeType.Charge) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Pick), CreateNode(AgvGraph.NodeType.Pick) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Pick), CreateNode(AgvGraph.NodeType.Park) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.PickDrop), CreateNode(AgvGraph.NodeType.WayPoint) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.PickDrop), CreateNode(AgvGraph.NodeType.Charge) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.PickDrop), CreateNode(AgvGraph.NodeType.Pick) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.PickDrop), CreateNode(AgvGraph.NodeType.Park) };
    }

    private static IEnumerable<object[]> NoSourcesGoesToDestination()
    {
        yield return new object[] { CreateNode(AgvGraph.NodeType.WayPoint), CreateNode(AgvGraph.NodeType.Drop) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.WayPoint), CreateNode(AgvGraph.NodeType.PickDrop) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.Charge), CreateNode(AgvGraph.NodeType.Drop) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Charge), CreateNode(AgvGraph.NodeType.PickDrop) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.Drop), CreateNode(AgvGraph.NodeType.Drop) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Drop), CreateNode(AgvGraph.NodeType.PickDrop) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.Park), CreateNode(AgvGraph.NodeType.Drop) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Park), CreateNode(AgvGraph.NodeType.PickDrop) };
    }

    private static IEnumerable<object[]> SourcesGoesToDestination()
    {
        yield return new object[] { CreateNode(AgvGraph.NodeType.Pick), CreateNode(AgvGraph.NodeType.Drop) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.PickDrop), CreateNode(AgvGraph.NodeType.Drop) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.Pick), CreateNode(AgvGraph.NodeType.PickDrop) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.PickDrop), CreateNode(AgvGraph.NodeType.PickDrop) };
    }

    private static IEnumerable<object[]> OnlyOneIsActionType()
    {
        yield return new object[] { CreateNode(AgvGraph.NodeType.Pick), CreateNode(AgvGraph.NodeType.WayPoint) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Drop), CreateNode(AgvGraph.NodeType.WayPoint) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.PickDrop), CreateNode(AgvGraph.NodeType.WayPoint) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.WayPoint), CreateNode(AgvGraph.NodeType.Pick) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.WayPoint), CreateNode(AgvGraph.NodeType.Drop) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.WayPoint), CreateNode(AgvGraph.NodeType.PickDrop) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.Pick), CreateNode(AgvGraph.NodeType.Charge) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Drop), CreateNode(AgvGraph.NodeType.Charge) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.PickDrop), CreateNode(AgvGraph.NodeType.Charge) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.Charge), CreateNode(AgvGraph.NodeType.Pick) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Charge), CreateNode(AgvGraph.NodeType.Drop) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Charge), CreateNode(AgvGraph.NodeType.PickDrop) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.Pick), CreateNode(AgvGraph.NodeType.Park) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Drop), CreateNode(AgvGraph.NodeType.Park) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.PickDrop), CreateNode(AgvGraph.NodeType.Park) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.Park), CreateNode(AgvGraph.NodeType.Pick) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Park), CreateNode(AgvGraph.NodeType.Drop) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Park), CreateNode(AgvGraph.NodeType.PickDrop) };
    }

    private static IEnumerable<object[]> NoneIsActionType()
    {
        yield return new object[] { CreateNode(AgvGraph.NodeType.WayPoint), CreateNode(AgvGraph.NodeType.WayPoint) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.WayPoint), CreateNode(AgvGraph.NodeType.Charge) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.WayPoint), CreateNode(AgvGraph.NodeType.Park) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.Charge), CreateNode(AgvGraph.NodeType.WayPoint) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Park), CreateNode(AgvGraph.NodeType.WayPoint) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.Charge), CreateNode(AgvGraph.NodeType.Charge) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Charge), CreateNode(AgvGraph.NodeType.WayPoint) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Charge), CreateNode(AgvGraph.NodeType.Park) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.WayPoint), CreateNode(AgvGraph.NodeType.Charge) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Park), CreateNode(AgvGraph.NodeType.Charge) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.Park), CreateNode(AgvGraph.NodeType.Park) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Park), CreateNode(AgvGraph.NodeType.Charge) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.Park), CreateNode(AgvGraph.NodeType.WayPoint) };

        yield return new object[] { CreateNode(AgvGraph.NodeType.Charge), CreateNode(AgvGraph.NodeType.Park) };
        yield return new object[] { CreateNode(AgvGraph.NodeType.WayPoint), CreateNode(AgvGraph.NodeType.Park) };
    }

    private static GraphNode CreateNode(AgvGraph.NodeType nodeType)
    {
        return new GraphNode(Guid.NewGuid().ToString())
        {
            Type = nodeType
        };
    }
}