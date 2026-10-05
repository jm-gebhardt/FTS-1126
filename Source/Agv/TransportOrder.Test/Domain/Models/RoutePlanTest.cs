using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Route;
using Shouldly;
using CommonAgvAction = Gebhardt.Acc.Common.Messages.ActionGroups.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class RoutePlanTest
{
    private static RoutePlan CreatePlan(int nodeCount)
    {
        var plan = RoutePlan.FromAction(new CommonAgvAction());
        var nodeIds = Enumerable.Range(0, nodeCount).Select(_ => Guid.NewGuid()).ToList();
        foreach (var id in nodeIds)
        {
            plan.Nodes.Add(new RouteNode(new GraphNode(id, id.ToString()), released: false));
        }
        for (int i = 0; i < nodeCount - 1; i++)
        {
            plan.Edges.Add(new RouteEdge(new GraphEdge(nodeIds[i], nodeIds[i + 1], 1f, Guid.NewGuid()), released: false));
        }
        return plan;
    }

    [TestMethod]
    public void ReleaseUntil_NullGraphNodeId_ReleasesAllNodes_ReturnsTrue()
    {
        var plan = CreatePlan(3);

        bool result = plan.ReleaseUntil(null);

        result.ShouldBeTrue();
        plan.Nodes.ShouldAllBe(n => n.Released);
        plan.Edges.ShouldAllBe(e => e.Released);
    }

    [TestMethod]
    public void ReleaseUntil_NullGraphNodeId_AllAlreadyReleased_ReturnsFalse()
    {
        var plan = CreatePlan(3);
        plan.ReleaseUntil(null);

        bool result = plan.ReleaseUntil(null);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public void ReleaseUntil_TargetNodeId_ReleasesUpToAndIncludingTarget_ReturnsTrue()
    {
        var plan = CreatePlan(3);
        var targetId = plan.Nodes[1].GraphNode.Id;

        bool result = plan.ReleaseUntil(targetId);

        result.ShouldBeTrue();
        plan.Nodes[0].Released.ShouldBeTrue();
        plan.Nodes[1].Released.ShouldBeTrue();
        plan.Nodes[2].Released.ShouldBeFalse();
    }

    [TestMethod]
    public void ReleaseUntil_TargetNodeId_ReleasesEdgeBetweenReleasedNodes()
    {
        var plan = CreatePlan(3);
        var targetId = plan.Nodes[1].GraphNode.Id;

        plan.ReleaseUntil(targetId);

        plan.Edges[0].Released.ShouldBeTrue();
        plan.Edges[1].Released.ShouldBeFalse();
    }

    [TestMethod]
    public void ReleaseUntil_TargetNodeIdNotFound_ReturnsFalse_NodesUnchanged()
    {
        var plan = CreatePlan(3);

        bool result = plan.ReleaseUntil(Guid.NewGuid());

        result.ShouldBeFalse();
        plan.Nodes.ShouldAllBe(n => !n.Released);
    }
}