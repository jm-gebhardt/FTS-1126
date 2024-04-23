using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Routing.Base;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Routing;

[TestClass]
public class NoCheckPlausibilityCheckerTest
{
    private IRoutingPlausibilityChecker _routingPlausibilityChecker = null!;

    [TestInitialize]
    public void Setup()
    {
        _routingPlausibilityChecker = new NoCheckPlausibilityChecker();
    }

    [DataTestMethod]
    [DynamicData(nameof(AllNodeToNodes), DynamicDataSourceType.Method)]
    public void IsPlausible_ReturnsTrue_WhereAnyParameter(GraphNode source, GraphNode destination)
    {
        bool isPlausible = _routingPlausibilityChecker.IsPlausible(source, destination);
        isPlausible.ShouldBeTrue();
    }

    private static IEnumerable<object[]> AllNodeToNodes()
    {
        foreach (var startNodeType in GetAllNodeTypes())
        {
            foreach (var endNodeType in GetAllNodeTypes())
            {
                yield return new object[] { CreateNode(startNodeType), CreateNode(endNodeType) };
            }
        }
    }

    private static AgvGraph.NodeType[] GetAllNodeTypes()
    {
        return Enum.GetValues<AgvGraph.NodeType>();
    }

    private static GraphNode CreateNode(AgvGraph.NodeType nodeType)
    {
        return new GraphNode(Guid.NewGuid().ToString())
        {
            Type = nodeType
        };
    }
}