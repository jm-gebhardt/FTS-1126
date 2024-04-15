using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.MqttMessages;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Extensions;

/// <summary>
/// Contains extension methods for mqtt message related records.
/// </summary>
public static class MqttMessagesExtensions
{
    /// <summary>
    /// Converts this <see cref="MqttControlPoint"/> into a <see cref="Vector3"/> by setting the Z component as 0.
    /// </summary>
    /// <param name="controlPoint">The control point to convert.</param>
    [UsedImplicitly]
    public static Vector3 AsVector3(this MqttControlPoint controlPoint)
    {
        return new Vector3(controlPoint.X, controlPoint.Y, 0);
    }

    /// <summary>
    /// Converts this <see cref="MqttNode"/> into a <see cref="Vector3"/> by setting the Z component as 0 if it does not exist on the original node.
    /// </summary>
    /// <param name="node">The node to convert.</param>
    [UsedImplicitly]
    public static Vector3 AsVector3(this MqttNode node)
    {
        return new Vector3(node.X, node.Y, node.Z ?? 0);
    }

    /// <summary>
    /// Converts this <see cref="MqttControlPoint"/> into a <see cref="Vector2"/>.
    /// </summary>
    /// <param name="controlPoint">The control point to convert.</param>
    [UsedImplicitly]
    public static Vector2 AsVector2(this MqttControlPoint controlPoint)
    {
        return new Vector2(controlPoint.X, controlPoint.Y);
    }

    /// <summary>
    /// Converts this <see cref="MqttNode"/> into a <see cref="Vector2"/>.
    /// </summary>
    /// <param name="node">The node to convert.</param>
    [UsedImplicitly]
    public static Vector2 AsVector2(this MqttNode node)
    {
        return new Vector2(node.X, node.Y);
    }

    /// <summary>
    /// Converts this <see cref="MqttEdge"/> into a <see cref="Direct"/>
    /// </summary>
    /// <param name="_">The edge.</param>
    /// <param name="start">The start <see cref="MqttNode"/></param>
    /// <param name="end">The end <see cref="MqttNode"/></param>
    public static Direct ToDirect(this MqttEdge _, MqttNode start, MqttNode end)
    {
        return new Direct(start.AsVector3(), end.AsVector3());
    }

    /// <summary>
    /// Converts this <see cref="MqttEdge"/> into a <see cref="Bezier"/>
    /// </summary>
    /// <param name="edge">The edge.</param>
    /// <param name="start">The start <see cref="MqttNode"/></param>
    /// <param name="end">The end <see cref="MqttNode"/></param>
    public static Bezier ToBezier(this MqttEdge edge, MqttNode start, MqttNode end)
    {
        var points = new List<Vector2> { start.AsVector2(), edge.ControlPoints!.First().AsVector2() };
        if (edge.ControlPoints!.Count == 2)
        {
            points.Add(edge.ControlPoints!.Last().AsVector2());
        }
        points.Add(end.AsVector2());
        return new Bezier(points);
    }

    /// <summary>
    /// Converts this <see cref="MqttEdge"/> into a <see cref="Polypath"/>
    /// </summary>
    /// <param name="edge">The edge.</param>
    /// <param name="start">The start <see cref="MqttNode"/></param>
    /// <param name="end">The end <see cref="MqttNode"/></param>
    public static Polypath ToPolypath(this MqttEdge edge, MqttNode start, MqttNode end)
    {
        var points = new List<Vector3> { start.AsVector3() };

        foreach (var controlPoint in edge.ControlPoints ?? new List<MqttControlPoint>())
        {
            points.Add(controlPoint.AsVector3());
        }
        points.Add(end.AsVector3());

        return new Polypath(points);
    }

    /// <summary>
    /// Converts this <see cref="MqttEdge"/> into a <see cref="Slanted"/>
    /// </summary>
    /// <param name="edge">The edge.</param>
    /// <param name="start">The start <see cref="MqttNode"/></param>
    /// <param name="end">The end <see cref="MqttNode"/></param>
    public static Slanted ToSlanted(this MqttEdge edge, MqttNode start, MqttNode end)
    {
        return new Slanted(start.AsVector3(), end.AsVector3(), edge.ExitAngle, edge.ExitDistance);
    }
}