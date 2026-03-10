using System.Numerics;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Microsoft.Extensions.Logging;
using TinySpline;

namespace Gebhardt.Acc.Common.Domain.Services;

/// <summary>
/// Provides an edge builder based on https://github.com/msteinbeck/tinyspline.
/// </summary>
public class TinySplineEdgeBuilder(ILogger<TinySplineEdgeBuilder> logger, TimeProvider timeProvider) : INurbsEdgeBuilder
{
    private const int BezierLengthResolution = 100;
    private const int PolyPathResolution = 12;
   
    /// <inheritdoc/>
    public GraphEdge BuildFromNurbs(Nurbs nurbs, Guid fromNodeId, Guid toNodeId, Guid? id = null, DateTime timeStamp = default)
    {
        if (nurbs.ControlPoints.Length < 2){
            throw new NurbsParsingException("NURBS must have at least 2 control points.");
        }
        if (timeStamp == default)
        {
            timeStamp = timeProvider.GetUtcNow().DateTime;
        }
        // Degree-1 is a polyline: control points are the exact curve points, no Bezier conversion needed.
        // Handled separately because ToBeziers() duplicates interior points for multi-segment degree-1 splines.
        if (nurbs.Degree == 1)
        {
            return CreateLinearEdge(nurbs, fromNodeId, toNodeId, id, timeStamp);
        }

        var spline = GetBSpline(nurbs);
        // ToBeziers() decomposes the B-spline into Bezier segments; for a single-segment clamped NURBS
        // the resulting degree equals the original NURBS degree.
        var beziers = spline.ToBeziers();

        ICollection<EdgeCoefficient> controlPoints = GetControlPoints(beziers);
        return CreateEdge(beziers.Degree, controlPoints, fromNodeId, toNodeId, id, timeStamp);
    }

    private GraphEdge CreateLinearEdge(Nurbs nurbs, Guid fromNodeId, Guid toNodeId, Guid? id, DateTime timeStamp)
    {
        double length = ComputePolylineLength(nurbs.ControlPoints);
        List<EdgeCoefficient> points = nurbs.ControlPoints.Select(cp => new EdgeCoefficient(cp.X, cp.Y)).ToList();

        if (points.Count == 2)
        {
            logger.LogDebug("Creating edge of type {EdgeType} from node {FromNodeId} to node {ToNodeId}", nameof(EdgeType.Direct), fromNodeId, toNodeId);
            return GraphEdge.CreateDirect(fromNodeId, toNodeId, (float)length, id, timeStamp);
        }

        List<EdgeCoefficient> innerPoints = points.GetInner().ToList();
        logger.LogDebug("Creating edge of type {EdgeType} from node {FromNodeId} to node {ToNodeId}", nameof(EdgeType.Polypath), fromNodeId, toNodeId);
        return GraphEdge.CreatePolypath(fromNodeId, toNodeId, (float)length, innerPoints, id, timeStamp);
    }

    private static double ComputePolylineLength(NurbsControlPoint[] points)
    {
        double length = 0;
        for (int i = 0; i < points.Length - 1; i++)
        {
            length += Mathematics.EuclideanDistance(points[i].AsVector3(), points[i + 1].AsVector3());
        }
        return length;
    }

    private GraphEdge CreateEdge(
        uint degree,
        ICollection<EdgeCoefficient> controlPoints,
        Guid fromNodeId, Guid toNodeId, Guid? id, DateTime timeStamp)
    {
        var bezier = new Bezier(controlPoints.Select(cp => cp.ToVector2()).ToList());
        List<EdgeCoefficient> innerPoints = controlPoints.GetInner().ToList();

        int expectedCount = (int)degree - 1;
        switch (degree)
        {
            case 2:
                if (innerPoints.Count != expectedCount)
                {
                    throw new NurbsParsingException($"Wrong number of control points ({innerPoints.Count}) for {nameof(EdgeType.Bezier2)} curve, expected {expectedCount}");
                }
                logger.LogDebug("Creating edge of type {EdgeType} from node {FromNodeId} to node {ToNodeId}", nameof(EdgeType.Bezier2), fromNodeId, toNodeId);
                return GraphEdge.CreateBezier2(fromNodeId, toNodeId, (float)bezier.GetLength(BezierLengthResolution), innerPoints.First().X!.Value, innerPoints.First().Y!.Value, id, timeStamp);

            case 3:
                if (innerPoints.Count != expectedCount)
                {
                    throw new NurbsParsingException($"Wrong number of control points ({innerPoints.Count}) for {nameof(EdgeType.Bezier3)} curve, expected {expectedCount}");
                }
                logger.LogDebug("Creating edge of type {EdgeType} from node {FromNodeId} to node {ToNodeId}", nameof(EdgeType.Bezier3), fromNodeId, toNodeId);
                return GraphEdge.CreateBezier3(fromNodeId, toNodeId, (float)bezier.GetLength(BezierLengthResolution), innerPoints.First().X!.Value, innerPoints.First().Y!.Value, innerPoints.Last().X!.Value, innerPoints.Last().Y!.Value, id, timeStamp);

            case 5:
                if (innerPoints.Count != expectedCount)
                {
                    throw new NurbsParsingException($"Wrong number of control points ({innerPoints.Count}) for {nameof(EdgeType.Bezier5)} curve, expected {expectedCount}");
                }
                logger.LogDebug("Creating edge of type {EdgeType} from node {FromNodeId} to node {ToNodeId}", nameof(EdgeType.Bezier5), fromNodeId, toNodeId);
                return GraphEdge.CreateBezier5(fromNodeId, toNodeId, (float)bezier.GetLength(BezierLengthResolution), innerPoints.ToArray(), id, timeStamp);

            default:
            {
                ICollection<Vector2> polypath = bezier.InterpolateCurve(PolyPathResolution);
                logger.LogDebug("Creating edge of type {EdgeType} from node {FromNodeId} to node {ToNodeId}", nameof(EdgeType.Polypath), fromNodeId, toNodeId);
                return GraphEdge.CreatePolypath(fromNodeId, toNodeId, (float)bezier.GetLength(BezierLengthResolution), polypath.Skip(1).Take(polypath.Count - 2).Select(p => new EdgeCoefficient(p.X, p.Y)).ToList(), id, timeStamp);
            }
        }
    }

    private BSpline GetBSpline(Nurbs nurbs)
    {
        // Rational NURBS (any weighted control point) require dimension 3 in homogeneous coordinates.
        uint dimension = nurbs.IsWeighted ? 3u : 2u;
        List<double> controlPointList = nurbs.IsWeighted
            ? nurbs.ControlPointsAsHomogeneousList()
            : nurbs.ControlPointsAsList();

        BSpline spline;
        if (nurbs.KnotVector.Length == 0)
        {
            // If the control points include start and end point of the curve, the type is 'Clamped':
            // It repeats the first and last knot 'degree + 1' times (e.g. [0,0,0, ..., 1,1,1] for degree 2).
            // This forces the curve to interpolate (pass through) the first and last control points. So P0 = curve start, P_n = curve end. 
            spline = new BSpline((uint)nurbs.ControlPoints.Length, dimension, (uint)nurbs.Degree, BSpline.Type.Clamped);
            spline.ControlPoints = controlPointList;
            return spline; 
        }
        spline = new BSpline((uint)nurbs.ControlPoints.Length, dimension, (uint)nurbs.Degree);
        spline.ControlPoints = controlPointList;
        spline.Knots = nurbs.KnotVector.ToList();
        return spline;
    }

    private ICollection<EdgeCoefficient> GetControlPoints(BSpline spline)
    {
        // For rational splines (dimension 3) control points are homogeneous [w*x, w*y, w, ...];
        // de-homogenize to get Cartesian coordinates.
        int dim = (int)spline.Dimension;
        var controlPoints = new List<EdgeCoefficient>();
        for (int i = 0; i < spline.ControlPoints.Count; i += dim)
        {
            double x, y;
            if (dim == 3)
            {
                double w = spline.ControlPoints[i + 2];
                x = spline.ControlPoints[i] / w;
                y = spline.ControlPoints[i + 1] / w;
            }
            else
            {
                x = spline.ControlPoints[i];
                y = spline.ControlPoints[i + 1];
            }
            controlPoints.Add(new EdgeCoefficient(x, y));
        }
        return controlPoints;
    }
}
