using System.Numerics;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Extensions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Extensions;

[TestClass]
public class MqttMessagesExtensionsTest
{
    [TestMethod]
    public void AsVector3_ControlPointMapsXyAndZeroZ()
    {
        var controlPoint = new MqttControlPoint { X = 1.5f, Y = -2.25f };

        var vector = controlPoint.AsVector3();

        vector.ShouldBe(new Vector3(1.5f, -2.25f, 0f));
    }

    [TestMethod]
    public void AsVector2_ControlPointMapsXy()
    {
        var controlPoint = new MqttControlPoint { X = -4f, Y = 7f };

        var vector = controlPoint.AsVector2();

        vector.ShouldBe(new Vector2(-4f, 7f));
    }

    [TestMethod]
    public void AsVector3_NodeUsesProvidedZ()
    {
        var node = new MqttNode { X = 2f, Y = 3f, Z = 4f };

        var vector = node.AsVector3();

        vector.ShouldBe(new Vector3(2f, 3f, 4f));
    }

    [TestMethod]
    public void AsVector3_NodeFallsBackToZeroZ()
    {
        var node = new MqttNode { X = 2f, Y = 3f, Z = null };

        var vector = node.AsVector3();

        vector.ShouldBe(new Vector3(2f, 3f, 0f));
    }

    [TestMethod]
    public void AsVector2_NodeMapsXy()
    {
        var node = new MqttNode { X = 5f, Y = -9f, Z = 1f };

        var vector = node.AsVector2();

        vector.ShouldBe(new Vector2(5f, -9f));
    }

    [TestMethod]
    public void ToDirect_ReturnsExpectedLength()
    {
        var start = new MqttNode { X = 0f, Y = 0f, Z = 1f };
        var end = new MqttNode { X = 3f, Y = 4f, Z = 1f };

        var direct = new MqttEdge().ToDirect(start, end);

        direct.GetLength().ShouldBe(5d, 0.000001d);
    }

    [TestMethod]
    public void ToBezier_InterpolatesFromStartToEnd()
    {
        var start = new MqttNode { X = 0f, Y = 0f };
        var end = new MqttNode { X = 3f, Y = 0f };
        var edge = new MqttEdge
        {
            ControlPoints = new[]
            {
                new MqttControlPoint { X = 1f, Y = 2f },
                new MqttControlPoint { X = 2f, Y = 2f }
            }
        };

        var bezier = edge.ToBezier(start, end);
        Vector2[] interpolation = bezier.InterpolateCurve(5).ToArray();

        interpolation.First().ShouldBe(new Vector2(0f, 0f));
        interpolation.Last().ShouldBe(new Vector2(3f, 0f));
    }

    [TestMethod]
    public void ToBezier_WithoutControlPointsThrows()
    {
        var start = new MqttNode { X = 0f, Y = 0f };
        var end = new MqttNode { X = 1f, Y = 0f };
        var edge = new MqttEdge { ControlPoints = null };

        Should.Throw<ArgumentOutOfRangeException>(() => edge.ToBezier(start, end));
    }

    [TestMethod]
    public void ToPolypath_WithControlPointsReturnsExpectedLength()
    {
        var start = new MqttNode { X = 0f, Y = 0f, Z = 0f };
        var end = new MqttNode { X = 6f, Y = 8f, Z = 0f };
        var edge = new MqttEdge
        {
            ControlPoints = new[]
            {
                new MqttControlPoint { X = 3f, Y = 0f },
                new MqttControlPoint { X = 3f, Y = 4f }
            }
        };

        var polypath = edge.ToPolypath(start, end);

        polypath.GetLength().ShouldBe(12d, 0.000001d);
    }

    [TestMethod]
    public void ToPolypath_WithoutControlPointsReturnsDirectDistance()
    {
        var start = new MqttNode { X = 1f, Y = 2f, Z = 0f };
        var end = new MqttNode { X = 4f, Y = 6f, Z = 0f };
        var edge = new MqttEdge { ControlPoints = null };

        var polypath = edge.ToPolypath(start, end);

        polypath.GetLength().ShouldBe(5d, 0.000001d);
    }

    [TestMethod]
    public void ToSlanted_WithZeroDistanceMatchesDirectDistance()
    {
        var start = new MqttNode { X = 0f, Y = 0f, Z = 0f };
        var end = new MqttNode { X = 3f, Y = 4f, Z = 0f };
        var edge = new MqttEdge { ExitAngle = 1.3f, ExitDistance = 0f };

        var slanted = edge.ToSlanted(start, end);

        slanted.GetLength().ShouldBe(5d, 0.000001d);
    }
}
