using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

[TestClass]
public class EdgeExtensionsTest
{
    [TestMethod]
    public void ApplyFallbackTrajectories_EdgeWithExistingTrajectory_Unchanged()
    {
        var trajectory = new Trajectory
        {
            Degree = 3,
            KnotVector = [0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f],
            ControlPoints =
            [
                new ControlPoint { X = 10, Y = 20 },
                new ControlPoint { X = 30, Y = 40 }
            ]
        };
        var edges = new List<Edge>
        {
            new()
            {
                EdgeId = "e1",
                StartNodeId = "n1",
                EndNodeId = "n2",
                AgvTrajectory = trajectory,
                AgvActions = []
            }
        };
        var nodes = new List<Node>
        {
            new() { NodeId = "n1", Position = new NodePosition(0, 0), AgvActions = [] },
            new() { NodeId = "n2", Position = new NodePosition(1, 1), AgvActions = [] }
        };

        edges.ApplyFallbackTrajectories(nodes);

        edges.Single().AgvTrajectory.Degree.ShouldBe(3);
        edges.Single().AgvTrajectory.ControlPoints.First().X.ShouldBe(10);
    }

    [TestMethod]
    public void ApplyFallbackTrajectories_EdgeWithoutTrajectory_GetsStraightLine()
    {
        var edges = new List<Edge>
        {
            new()
            {
                EdgeId = "e1",
                StartNodeId = "n1",
                EndNodeId = "n2",
                AgvTrajectory = new Trajectory(),
                AgvActions = []
            }
        };
        var nodes = new List<Node>
        {
            new() { NodeId = "n1", Position = new NodePosition(1.0, 2.0), AgvActions = [] },
            new() { NodeId = "n2", Position = new NodePosition(3.0, 4.0), AgvActions = [] }
        };

        edges.ApplyFallbackTrajectories(nodes);

        var traj = edges.Single().AgvTrajectory;
        traj.Degree.ShouldBe(1);
        traj.KnotVector.ShouldBe([0f, 0f, 1f, 1f]);
        traj.ControlPoints.Count.ShouldBe(2);
        traj.ControlPoints.First().X.ShouldBe(1.0);
        traj.ControlPoints.First().Y.ShouldBe(2.0);
        traj.ControlPoints.Last().X.ShouldBe(3.0);
        traj.ControlPoints.Last().Y.ShouldBe(4.0);
    }

    [TestMethod]
    public void ApplyFallbackTrajectories_MixedEdges_OnlyFillsEmpty()
    {
        var existingTrajectory = new Trajectory
        {
            Degree = 2,
            KnotVector = [0f, 0f, 1f, 1f],
            ControlPoints = [new ControlPoint { X = 99, Y = 99 }]
        };
        var edges = new List<Edge>
        {
            new() { EdgeId = "e1", StartNodeId = "n1", EndNodeId = "n2", AgvTrajectory = existingTrajectory, AgvActions = [] },
            new() { EdgeId = "e2", StartNodeId = "n2", EndNodeId = "n3", AgvTrajectory = new Trajectory(), AgvActions = [] }
        };
        var nodes = new List<Node>
        {
            new() { NodeId = "n1", Position = new NodePosition(0, 0), AgvActions = [] },
            new() { NodeId = "n2", Position = new NodePosition(1, 1), AgvActions = [] },
            new() { NodeId = "n3", Position = new NodePosition(2, 2), AgvActions = [] }
        };

        edges.ApplyFallbackTrajectories(nodes);

        edges[0].AgvTrajectory.Degree.ShouldBe(2);
        edges[0].AgvTrajectory.ControlPoints.First().X.ShouldBe(99);
        edges[1].AgvTrajectory.Degree.ShouldBe(1);
        edges[1].AgvTrajectory.ControlPoints.Count.ShouldBe(2);
    }
}