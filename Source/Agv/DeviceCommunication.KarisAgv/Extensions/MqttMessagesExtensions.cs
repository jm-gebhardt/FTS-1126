using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Extensions;

/// <summary>
/// Contains extension methods for mqtt message related records.
/// </summary>
public static class MqttMessagesExtensions
{
    /// <summary>
    /// Extension block for <see cref="MqttControlPoint"/>.
    /// </summary>
    extension(MqttControlPoint controlPoint)
    {
        /// <summary>
        /// Converts this <see cref="MqttControlPoint"/> into a <see cref="Vector3"/> by setting the Z component as 0.
        /// </summary>
        [UsedImplicitly]
        public Vector3 AsVector3()
        {
            return new Vector3(controlPoint.X, controlPoint.Y, 0);
        }

        /// <summary>
        /// Converts this <see cref="MqttControlPoint"/> into a <see cref="Vector2"/>.
        /// </summary>
        [UsedImplicitly]
        public Vector2 AsVector2()
        {
            return new Vector2(controlPoint.X, controlPoint.Y);
        }
    }

    /// <summary>
    /// Extension block for <see cref="MqttNode"/>.
    /// </summary>
    extension(MqttNode node)
    {
        /// <summary>
        /// Converts this <see cref="MqttNode"/> into a <see cref="Vector3"/> by setting the Z component as 0 if it does not exist on the original node.
        /// </summary>
        [UsedImplicitly]
        public Vector3 AsVector3()
        {
            return new Vector3(node.X, node.Y, node.Z ?? 0);
        }

        /// <summary>
        /// Converts this <see cref="MqttNode"/> into a <see cref="Vector2"/>.
        /// </summary>
        [UsedImplicitly]
        public Vector2 AsVector2()
        {
            return new Vector2(node.X, node.Y);
        }
    }

    /// <summary>
    /// Extension block for <see cref="MqttEdge"/>.
    /// </summary>
    extension(MqttEdge edge)
    {
        /// <summary>
        /// Converts this <see cref="MqttEdge"/> into a <see cref="Direct"/>
        /// </summary>
        /// <param name="start">The start <see cref="MqttNode"/></param>
        /// <param name="end">The end <see cref="MqttNode"/></param>
        public Direct ToDirect(MqttNode start, MqttNode end)
        {
            return new Direct(start.AsVector3(), end.AsVector3());
        }

        /// <summary>
        /// Converts this <see cref="MqttEdge"/> into a <see cref="Bezier"/>
        /// </summary>
        /// <param name="start">The start <see cref="MqttNode"/></param>
        /// <param name="end">The end <see cref="MqttNode"/></param>
        public Bezier ToBezier(MqttNode start, MqttNode end)
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
        /// <param name="start">The start <see cref="MqttNode"/></param>
        /// <param name="end">The end <see cref="MqttNode"/></param>
        public Polypath ToPolypath(MqttNode start, MqttNode end)
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
        /// <param name="start">The start <see cref="MqttNode"/></param>
        /// <param name="end">The end <see cref="MqttNode"/></param>
        public Slanted ToSlanted(MqttNode start, MqttNode end)
        {
            return new Slanted(start.AsVector3(), end.AsVector3(), edge.ExitAngle, edge.ExitDistance);
        }
    }
}
