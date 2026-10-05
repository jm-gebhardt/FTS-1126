using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;
using Shouldly;
using OrientationType = Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders.OrientationType;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Domain;

[TestClass]
public class OrderEdgeTest
{
    private static OrderNode GetOrderNode(double x = 0, double y = 0)
    {
        return new OrderNode() { NodePosition = new OrderNodePosition() { X = x, Y = y } };
    }

    [TestMethod]
    public void Release_WhenNotReleased_SetsReleasedToTrue()
    {
        var edge = new OrderEdge { Released = false };

        edge.Release();

        edge.Released.ShouldBeTrue();
    }
    
    [TestMethod]
    public void Prepare_WithZeroSpeed_FallsBackToOneSecond()
    {
        var edge = new OrderEdge();

        edge.Prepare(0.0f, GetOrderNode(), GetOrderNode());

        edge.EstimatedTravelTime.ShouldBe(TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Prepare_WithTrajectoryWithNoControlPoints_FallsBackToOneSecond()
    {
        // ToNurbs() returns null when ControlPoints is empty → fallback
        var edge = new OrderEdge { Trajectory = new Trajectory() };

        edge.Prepare(1.0f, GetOrderNode(), GetOrderNode());

        edge.EstimatedTravelTime.ShouldBe(TimeSpan.FromSeconds(1));
    }
    
    [TestMethod]
    public void Prepare_WithNurbsTrajectoryAndValidGraphEdgeLength_SetsEstimatedTravelTimeFromGraphEdge()
    {
        var edge = new OrderEdge
        {
            StartNodeId = "A",
            EndNodeId = "B",
            Trajectory = new Trajectory
            {
                ControlPoints =
                [
                    new ControlPoint { X = 0, Y = 0 },
                    new ControlPoint { X = 0, Y = 10 }
                ]
            }
        };

        edge.Prepare(2.0f, GetOrderNode(), GetOrderNode());

        edge.EstimatedTravelTime.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void GetPosition_WithoutTrajectory_ReturnsZeroPosition()
    {
        var edge = new OrderEdge();

        var position = edge.GetPosition(0.5, GetOrderNode(), GetOrderNode());

        position.X.ShouldBe(0f);
        position.Y.ShouldBe(0f);
        position.Theta.ShouldBe(0f);
        position.PositionInitialized.ShouldBeTrue();
    }

    [TestMethod]
    public void GetPosition_WithOrientationGlobalSet_ReturnsThetaAsOrientation()
    {
        var edge = new OrderEdge { Orientation = 1.57, OrientationType = OrientationType.GLOBAL};

        var position = edge.GetPosition(0.5, GetOrderNode(), GetOrderNode());

        position.Theta.ShouldBe((float)1.57);
    }

    [TestMethod]
    public void GetPosition_WithOrientationTangentialSet_ReturnsThetaPlusEdgeOrientationAsOrientation()
    {
        var edge = new OrderEdge { Orientation = 1.57, OrientationType = OrientationType.TANGENTIAL };

        var position = edge.GetPosition(0.5, GetOrderNode(), GetOrderNode(0, 1));

        position.Theta.ShouldBe((float)3.14, 1e-2);
    }

    [TestMethod]
    public void GetPosition_WithOrientationAndNoOrientationTypeSet_ReturnsThetaPlusEdgeOrientationAsOrientation()
    {
        var edge = new OrderEdge { Orientation = 1.57 };

        var position = edge.GetPosition(0.5, GetOrderNode(), GetOrderNode(0,1));

        position.Theta.ShouldBe((float)3.14, 1e-2);
    }

    [TestMethod]
    public void GetPosition_WithOrientationSetAndNoTrajectory_ReturnsThetaAsOrientation()
    {
        var edge = new OrderEdge { Orientation = 1.57 };

        var position = edge.GetPosition(0.5, GetOrderNode(), GetOrderNode());

        position.Theta.ShouldBe((float)1.57);
    }
    
    [TestMethod]
    public void GetPosition_WithLinearTrajectory_AtFractionZero_ReturnsStartPoint()
    {
        var edge = CreateHorizontalLinearEdge(fromX: 0, fromY: 0, toX: 10, toY: 0, length: 10);

        var position = edge.GetPosition(0.0, GetOrderNode(), GetOrderNode());

        position.X.ShouldBe(0f);
        position.Y.ShouldBe(0f);
        position.PositionInitialized.ShouldBeTrue();
    }

    [TestMethod]
    public void GetPosition_WithLinearTrajectory_AtFractionHalf_ReturnsMidpoint()
    {
        var edge = CreateHorizontalLinearEdge(fromX: 0, fromY: 0, toX: 10, toY: 0, length: 10);

        var position = edge.GetPosition(0.5, GetOrderNode(), GetOrderNode());

        position.X.ShouldBe(5f);
        position.Y.ShouldBe(0f);
    }

    [TestMethod]
    public void GetPosition_WithLinearTrajectory_AtFractionOne_ReturnsEndPoint()
    {
        var edge = CreateHorizontalLinearEdge(fromX: 0, fromY: 0, toX: 10, toY: 0, length: 10);

        var position = edge.GetPosition(1.0, GetOrderNode(), GetOrderNode());

        position.X.ShouldBe(10f);
        position.Y.ShouldBe(0f);
    }

    [TestMethod]
    public void GetPosition_WithVerticalLinearTrajectory_AtFractionHalf_ReturnsMidpoint()
    {
        // deltaX == 0 → uses vertical interpolation branch
        var edge = new OrderEdge
        {
            Trajectory = new Trajectory
            {
                ControlPoints =
                [
                    new ControlPoint { X = 0, Y = 0 },
                    new ControlPoint { X = 0, Y = 10 }
                ]
            }
        };

        var position = edge.GetPosition(0.5, GetOrderNode(), GetOrderNode());

        position.X.ShouldBe(0f);
        position.Y.ShouldBe(5f);
    }

    [TestMethod]
    public void GetPosition_WithLinearTrajectory_FractionBelowZero_ClampsToStartPoint()
    {
        var edge = CreateHorizontalLinearEdge(fromX: 0, fromY: 0, toX: 10, toY: 0, length: 10);

        var position = edge.GetPosition(-1.0, GetOrderNode(), GetOrderNode());

        position.X.ShouldBe(0f);
    }

    [TestMethod]
    public void GetPosition_WithLinearTrajectory_FractionAboveOne_ClampsToEndPoint()
    {
        var edge = CreateHorizontalLinearEdge(fromX: 0, fromY: 0, toX: 10, toY: 0, length: 10);

        var position = edge.GetPosition(2.0, GetOrderNode(), GetOrderNode());

        position.X.ShouldBe(10f);
    }

    [TestMethod]
    public void ActionIds_WithNoActions_ReturnsEmpty()
    {
        var edge = new OrderEdge();

        edge.ActionIds.ShouldBeEmpty();
    }

    [TestMethod]
    public void ActionIds_WithActions_ReturnsAllActionIds()
    {
        var action1 = new OrderAction(Vda5050Specifications.ActionTypes.Pick, null, BlockingType.NONE, []);
        var action2 = new OrderAction(Vda5050Specifications.ActionTypes.Drop, null, BlockingType.NONE, []);
        var edge = new OrderEdge { Actions = [action1, action2] };

        var ids = edge.ActionIds;

        ids.Count.ShouldBe(2);
        ids.ShouldContain(action1.ActionId);
        ids.ShouldContain(action2.ActionId);
    }

    private static OrderEdge CreateHorizontalLinearEdge(double fromX, double fromY, double toX, double toY, double length)
        => new()
        {
            Length = length,
            Trajectory = new Trajectory
            {
                ControlPoints =
                [
                    new ControlPoint { X = fromX, Y = fromY },
                    new ControlPoint { X = toX, Y = toY }
                ]
            }
        };
}
