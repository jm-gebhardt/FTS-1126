using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Testing.Time;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Services;

[TestClass]
public class TinySplineEdgeBuilderTest
{
    private TinySplineEdgeBuilder _builder = null!;

    [TestInitialize]
    public void Setup()
    {
        _builder = new TinySplineEdgeBuilder(new Mock<ILogger<TinySplineEdgeBuilder>>().Object, new FakeTimeProvider());
    }

    [TestMethod]
    public void BuildFromNurbs_LessThan2ControlPoints_ThrowsNurbsParsingException()
    {
        var degeneratedNurbs = new Nurbs
        {
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0)
            ]
        };
        Should.Throw<NurbsParsingException>(() =>
            _builder.BuildFromNurbs(degeneratedNurbs, Guid.NewGuid(), Guid.NewGuid()));
    }

    [TestMethod]
    public void BuildFromNurbs_Bezier5WithKnots_ReturnsBezier5()
    {
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var nurbs = new Nurbs
        {
            Degree = 5,
            KnotVector =
            [
                0.0, 0.0, 0.0, 0.0, 0.0, 0.0, // d+1 = 6 repeated start knots
                1.0, 1.0, 1.0, 1.0, 1.0, 1.0  // d+1 = 6 repeated end knots
            ],
            ControlPoints =
            [
                new NurbsControlPoint(-6.4865827, 4.9478641), // P0 (start)
                new NurbsControlPoint(-6.4865827, 5.8119168), // P1
                new NurbsControlPoint(-6.4865827, 6.6759696), // P2
                new NurbsControlPoint(-3.7837676, 7.0685949), // P3
                new NurbsControlPoint(-3.3021741, 6.6089205), // P4
                new NurbsControlPoint(-2.8371802, 6.7786427)  // P5 (end)
            ]
        };
        var graphEdge = _builder.BuildFromNurbs(nurbs, startNodeId, endNodeId, id, timestamp);

        graphEdge.Id.ShouldBe(id);
        graphEdge.FromNodeId.ShouldBe(startNodeId);
        graphEdge.ToNodeId.ShouldBe(endNodeId);
        graphEdge.Variant.ShouldBe(EdgeType.Bezier5);
        graphEdge.TimeStamp.ShouldBe(timestamp);
        graphEdge.Length.ShouldBeGreaterThan(0);
        graphEdge.VariantParameters.ShouldBeEquivalentTo(
            nurbs.ControlPoints.Skip(1).Take(4).Select(p => new EdgeCoefficient(p.X, p.Y)).ToArray());
    }

    [TestMethod]
    public void BuildFromNurbs_Bezier5WithoutKnots_AssumesClamped_ReturnsBezier5()
    {
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var nurbsNoKnots = new Nurbs
        {
            Degree = 5,
            KnotVector = [],
            ControlPoints =
            [
                new NurbsControlPoint(-6.4865827, 4.9478641), // P0 (start)
                new NurbsControlPoint(-6.4865827, 5.8119168), // P1
                new NurbsControlPoint(-6.4865827, 6.6759696), // P2
                new NurbsControlPoint(-3.7837676, 7.0685949), // P3
                new NurbsControlPoint(-3.3021741, 6.6089205), // P4
                new NurbsControlPoint(-2.8371802, 6.7786427)  // P5 (end)
            ]
        };

        var graphEdge = _builder.BuildFromNurbs(nurbsNoKnots, startNodeId, endNodeId, id, timestamp);

        graphEdge.Id.ShouldBe(id);
        graphEdge.FromNodeId.ShouldBe(startNodeId);
        graphEdge.ToNodeId.ShouldBe(endNodeId);
        graphEdge.Variant.ShouldBe(EdgeType.Bezier5);
        graphEdge.TimeStamp.ShouldBe(timestamp);
        graphEdge.Length.ShouldBeGreaterThan(0);
        graphEdge.VariantParameters.Count.ShouldBe(4);
    }

    [TestMethod]
    public void BuildFromNurbs_Bezier5WithMultipleSegments_ThrowsNurbsParsingException()
    {
        // Degree-5 B-Spline with one interior knot → ToBeziers() produces 2 segments → wrong control point count for case 5
        // KnotVector: 13 knots → #controlPoints = 13 - 5 - 1 = 7
        var bezier5MultiSegmentNurbs = new Nurbs
        {
            Degree = 5,
            KnotVector = [0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0],
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0), // P0
                new NurbsControlPoint(0.2, 1.0), // P1
                new NurbsControlPoint(0.5, 1.5), // P2
                new NurbsControlPoint(1.0, 0.5), // P3
                new NurbsControlPoint(1.5, 1.5), // P4
                new NurbsControlPoint(1.8, 1.0), // P5
                new NurbsControlPoint(2.0, 0.0)  // P6
            ]
        };
        Should.Throw<NurbsParsingException>(() =>
            _builder.BuildFromNurbs(bezier5MultiSegmentNurbs, Guid.NewGuid(), Guid.NewGuid()));
    }

    [TestMethod]
    public void BuildFromNurbs_Bezier2WithKnots_ReturnsBezier2()
    {
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var nurbs = new Nurbs
        {
            Degree = 2,
            KnotVector = [0.0, 0.0, 0.0, 1.0, 1.0, 1.0], // d+1=3 repeated at each end
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0), // P0 (start)
                new NurbsControlPoint(1.0, 1.0), // P1 (control)
                new NurbsControlPoint(2.0, 0.0)  // P2 (end)
            ]
        };

        var graphEdge = _builder.BuildFromNurbs(nurbs, startNodeId, endNodeId, id, timestamp);

        graphEdge.Id.ShouldBe(id);
        graphEdge.FromNodeId.ShouldBe(startNodeId);
        graphEdge.ToNodeId.ShouldBe(endNodeId);
        graphEdge.Variant.ShouldBe(EdgeType.Bezier2);
        graphEdge.TimeStamp.ShouldBe(timestamp);
        graphEdge.Length.ShouldBeGreaterThan(0);
        graphEdge.VariantParameters.Count.ShouldBe(1);
        graphEdge.VariantParameters.First().X!.Value.ShouldBe(1.0, tolerance: 1e-4);
        graphEdge.VariantParameters.First().Y!.Value.ShouldBe(1.0, tolerance: 1e-4);
    }

    [TestMethod]
    public void BuildFromNurbs_Bezier2WithoutKnots_AssumesClamped_ReturnsBezier2()
    {
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var nurbsNoKnots = new Nurbs
        {
            Degree = 2,
            KnotVector = [],
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0), // P0 (start)
                new NurbsControlPoint(1.0, 1.0), // P1 (control)
                new NurbsControlPoint(2.0, 0.0)  // P2 (end)
            ]
        };
        var graphEdge = _builder.BuildFromNurbs(nurbsNoKnots, startNodeId, endNodeId, id, timestamp);

        graphEdge.Id.ShouldBe(id);
        graphEdge.FromNodeId.ShouldBe(startNodeId);
        graphEdge.ToNodeId.ShouldBe(endNodeId);
        graphEdge.Variant.ShouldBe(EdgeType.Bezier2);
        graphEdge.TimeStamp.ShouldBe(timestamp);
        graphEdge.Length.ShouldBeGreaterThan(0);
        graphEdge.VariantParameters.Count.ShouldBe(1);
        graphEdge.VariantParameters.First().X!.Value.ShouldBe(1.0, tolerance: 1e-4);
        graphEdge.VariantParameters.First().Y!.Value.ShouldBe(1.0, tolerance: 1e-4);
    }

    [TestMethod]
    public void BuildFromNurbs_Bezier2WithMultipleSegments_ThrowsNurbsParsingException()
    {
        // Degree-2 B-Spline with one interior knot → ToBeziers() produces 2 segments → wrong control point count for case 2
        // KnotVector: 7 knots → #controlPoints = 7 - 2 - 1 = 4
        var bezier2MultiSegmentNurbs = new Nurbs
        {
            Degree = 2,
            KnotVector = [0.0, 0.0, 0.0, 0.5, 1.0, 1.0, 1.0],
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0), // P0
                new NurbsControlPoint(0.5, 1.0), // P1
                new NurbsControlPoint(1.0, 0.0), // P2
                new NurbsControlPoint(2.0, 0.0)  // P3
            ]
        };
        Should.Throw<NurbsParsingException>(() =>
            _builder.BuildFromNurbs(bezier2MultiSegmentNurbs, Guid.NewGuid(), Guid.NewGuid()));
    }

    [TestMethod]
    public void BuildFromNurbs_Bezier3WithKnots_ReturnsBezier3()
    {
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var nurbs = new Nurbs
        {
            Degree = 3,
            KnotVector = [0.0, 0.0, 0.0, 0.0, 1.0, 1.0, 1.0, 1.0], // d+1=4 repeated at each end
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0), // P0 (start)
                new NurbsControlPoint(0.5, 1.0), // P1
                new NurbsControlPoint(1.5, 1.0), // P2
                new NurbsControlPoint(2.0, 0.0)  // P3 (end)
            ]
        };

        var graphEdge = _builder.BuildFromNurbs(nurbs, startNodeId, endNodeId, id, timestamp);

        graphEdge.Id.ShouldBe(id);
        graphEdge.FromNodeId.ShouldBe(startNodeId);
        graphEdge.ToNodeId.ShouldBe(endNodeId);
        graphEdge.Variant.ShouldBe(EdgeType.Bezier3);
        graphEdge.TimeStamp.ShouldBe(timestamp);
        graphEdge.Length.ShouldBeGreaterThan(0);
        graphEdge.VariantParameters.Count.ShouldBe(2);
        graphEdge.VariantParameters.First().X!.Value.ShouldBe(0.5, tolerance: 1e-4);
        graphEdge.VariantParameters.First().Y!.Value.ShouldBe(1.0, tolerance: 1e-4);
        graphEdge.VariantParameters.Last().X!.Value.ShouldBe(1.5, tolerance: 1e-4);
        graphEdge.VariantParameters.Last().Y!.Value.ShouldBe(1.0, tolerance: 1e-4);
    }

    [TestMethod]
    public void BuildFromNurbs_Bezier3WithoutKnots_ReturnsBezier3()
    {
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var nurbsNoKnots = new Nurbs
        {
            Degree = 3,
            KnotVector = [],
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0), // P0 (start)
                new NurbsControlPoint(0.5, 1.0), // P1
                new NurbsControlPoint(1.5, 1.0), // P2
                new NurbsControlPoint(2.0, 0.0)  // P3 (end)
            ]
        };

        var graphEdge = _builder.BuildFromNurbs(nurbsNoKnots, startNodeId, endNodeId, id, timestamp);

        graphEdge.Id.ShouldBe(id);
        graphEdge.FromNodeId.ShouldBe(startNodeId);
        graphEdge.ToNodeId.ShouldBe(endNodeId);
        graphEdge.Variant.ShouldBe(EdgeType.Bezier3);
        graphEdge.TimeStamp.ShouldBe(timestamp);
        graphEdge.Length.ShouldBeGreaterThan(0);
        graphEdge.VariantParameters.Count.ShouldBe(2);
        graphEdge.VariantParameters.First().X!.Value.ShouldBe(0.5, tolerance: 1e-4);
        graphEdge.VariantParameters.First().Y!.Value.ShouldBe(1.0, tolerance: 1e-4);
        graphEdge.VariantParameters.Last().X!.Value.ShouldBe(1.5, tolerance: 1e-4);
        graphEdge.VariantParameters.Last().Y!.Value.ShouldBe(1.0, tolerance: 1e-4);
    }

    [TestMethod]
    public void BuildFromNurbs_Bezier3WithMultipleSegments_ThrowsNurbsParsingException()
    {
        // Degree-3 B-Spline with one interior knot → ToBeziers() produces 2 segments → wrong control point count for case 3
        // KnotVector: 9 knots → #controlPoints = 9 - 3 - 1 = 5
        var bezier3MultiSegmentNurbs = new Nurbs
        {
            Degree = 3,
            KnotVector = [0.0, 0.0, 0.0, 0.0, 0.5, 1.0, 1.0, 1.0, 1.0],
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0), // P0
                new NurbsControlPoint(0.3, 1.0), // P1
                new NurbsControlPoint(0.7, 1.5), // P2
                new NurbsControlPoint(1.3, 1.0), // P3
                new NurbsControlPoint(2.0, 0.0)  // P4
            ]
        };
        Should.Throw<NurbsParsingException>(() =>
            _builder.BuildFromNurbs(bezier3MultiSegmentNurbs, Guid.NewGuid(), Guid.NewGuid()));
    }

    [TestMethod]
    public void BuildFromNurbs_Degree4Nurbs_ReturnsPolypath()
    {
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        // Degree-4 B-Spline: hits the default switch branch → Polypath edge
        // InterpolateCurve(12) produces 12 points; skip first and last → 10 inner points (at the CreatePolypath limit)
        var nurbs = new Nurbs
        {
            Degree = 4,
            KnotVector = [], // clamped: [0,0,0,0,0, 1,1,1,1,1]
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0), // P0 (start)
                new NurbsControlPoint(0.5, 1.5), // P1
                new NurbsControlPoint(1.0, 0.5), // P2
                new NurbsControlPoint(1.5, 1.5), // P3
                new NurbsControlPoint(2.0, 0.0)  // P4 (end)
            ]
        };

        var graphEdge = _builder.BuildFromNurbs(nurbs, startNodeId, endNodeId, id, timestamp);

        graphEdge.Id.ShouldBe(id);
        graphEdge.FromNodeId.ShouldBe(startNodeId);
        graphEdge.ToNodeId.ShouldBe(endNodeId);
        graphEdge.Variant.ShouldBe(EdgeType.Polypath);
        graphEdge.TimeStamp.ShouldBe(timestamp);
        graphEdge.Length.ShouldBeGreaterThan(0);
        graphEdge.VariantParameters.Count.ShouldBe(10); // InterpolateCurve(12) → 12 points → skip first+last = 10
        graphEdge.VariantParameters.ShouldAllBe(p => p.IsPoint);
    }

    [TestMethod]
    public void BuildFromNurbs_DefaultTimestamp_SetsTimestampToUtcNow()
    {
        var before = DateTime.UtcNow;
        var nurbs = new Nurbs
        {
            Degree = 5,
            KnotVector = [],
            ControlPoints =
            [
                new NurbsControlPoint(-6.4865827, 4.9478641), // P0 (start)
                new NurbsControlPoint(-6.4865827, 5.8119168), // P1
                new NurbsControlPoint(-6.4865827, 6.6759696), // P2
                new NurbsControlPoint(-3.7837676, 7.0685949), // P3
                new NurbsControlPoint(-3.3021741, 6.6089205), // P4
                new NurbsControlPoint(-2.8371802, 6.7786427)  // P5 (end)
            ]
        };
        var graphEdge = _builder.BuildFromNurbs(nurbs, Guid.NewGuid(), Guid.NewGuid(), timeStamp: default);

        var after = DateTime.UtcNow;
        graphEdge.TimeStamp.ShouldBeGreaterThanOrEqualTo(before);
        graphEdge.TimeStamp.ShouldBeLessThanOrEqualTo(after);
    }

    [TestMethod]
    public void BuildFromNurbs_DefaultId_GeneratesNewNonEmptyGuid()
    {
        var nurbs = new Nurbs
        {
            Degree = 5,
            KnotVector = [],
            ControlPoints =
            [
                new NurbsControlPoint(-6.4865827, 4.9478641), // P0 (start)
                new NurbsControlPoint(-6.4865827, 5.8119168), // P1
                new NurbsControlPoint(-6.4865827, 6.6759696), // P2
                new NurbsControlPoint(-3.7837676, 7.0685949), // P3
                new NurbsControlPoint(-3.3021741, 6.6089205), // P4
                new NurbsControlPoint(-2.8371802, 6.7786427)  // P5 (end)
            ]
        };
        var graphEdge = _builder.BuildFromNurbs(nurbs, Guid.NewGuid(), Guid.NewGuid(), id: null);

        graphEdge.Id.ShouldNotBe(Guid.Empty);
    }

    [TestMethod]
    public void BuildFromNurbs_WeightedBezier2_ReturnsValidBezier2Edge()
    {
        // A degree-2 rational NURBS with unit weights must produce the same edge type as unweighted.
        var nurbs = new Nurbs
        {
            Degree = 2,
            KnotVector = [0.0, 0.0, 0.0, 1.0, 1.0, 1.0],
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0, 1.0), // P0 (start)
                new NurbsControlPoint(1.0, 1.0, 1.0), // P1 (control)
                new NurbsControlPoint(2.0, 0.0, 1.0)  // P2 (end)
            ]
        };

        var graphEdge = _builder.BuildFromNurbs(nurbs, Guid.NewGuid(), Guid.NewGuid());

        graphEdge.Variant.ShouldBe(EdgeType.Bezier2);
        graphEdge.Length.ShouldBeGreaterThan(0);
        graphEdge.VariantParameters.Count.ShouldBe(1);
        graphEdge.VariantParameters.First().X!.Value.ShouldBe(1.0, tolerance: 1e-4);
        graphEdge.VariantParameters.First().Y!.Value.ShouldBe(1.0, tolerance: 1e-4);
    }

    [TestMethod]
    public void BuildFromNurbs_WeightedBezier2_NullWeightsDefaultToOne()
    {
        // Mixing null and explicit weights: null must be treated as 1.0.
        // The resulting edge should be identical to the fully-weighted unit case.
        var nurbs = new Nurbs
        {
            Degree = 2,
            KnotVector = [0.0, 0.0, 0.0, 1.0, 1.0, 1.0],
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0, 1.0), // P0 — explicit weight
                new NurbsControlPoint(1.0, 1.0),       // P1 — null weight → treated as 1.0
                new NurbsControlPoint(2.0, 0.0, 1.0)  // P2 — explicit weight
            ]
        };

        var graphEdge = _builder.BuildFromNurbs(nurbs, Guid.NewGuid(), Guid.NewGuid());

        graphEdge.Variant.ShouldBe(EdgeType.Bezier2);
        graphEdge.Length.ShouldBeGreaterThan(0);
        graphEdge.VariantParameters.First().X!.Value.ShouldBe(1.0, tolerance: 1e-4);
        graphEdge.VariantParameters.First().Y!.Value.ShouldBe(1.0, tolerance: 1e-4);
    }

    [TestMethod]
    public void ControlPointsAsHomogeneousList_ZeroWeight_ThrowsNurbsParsingException()
    {
        var nurbs = new Nurbs
        {
            Degree = 2,
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0, 1.0),
                new NurbsControlPoint(1.0, 1.0, 0.0), // invalid zero weight
                new NurbsControlPoint(2.0, 0.0, 1.0)
            ]
        };

        Should.Throw<NurbsParsingException>(nurbs.ControlPointsAsHomogeneousList);
    }

    [TestMethod]
    public void BuildFromNurbs_ZeroWeight_ThrowsNurbsParsingException()
    {
        var nurbs = new Nurbs
        {
            Degree = 2,
            KnotVector = [0.0, 0.0, 0.0, 1.0, 1.0, 1.0],
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0, 1.0),
                new NurbsControlPoint(1.0, 1.0, 0.0), // invalid zero weight
                new NurbsControlPoint(2.0, 0.0, 1.0)
            ]
        };

        Should.Throw<NurbsParsingException>(() =>
            _builder.BuildFromNurbs(nurbs, Guid.NewGuid(), Guid.NewGuid()));
    }

    [TestMethod]
    public void BuildFromNurbs_Degree1SingleSegment_ReturnsDirect()
    {
        // A degree-1 NURBS with 2 control points is a straight line → Direct edge.
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var nurbs = new Nurbs
        {
            Degree = 1,
            KnotVector = [0.0, 0.0, 1.0, 1.0], // d+1=2 repeated at each end
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0), // P0 (start)
                new NurbsControlPoint(3.0, 4.0)  // P1 (end)
            ]
        };

        var graphEdge = _builder.BuildFromNurbs(nurbs, startNodeId, endNodeId);

        graphEdge.FromNodeId.ShouldBe(startNodeId);
        graphEdge.ToNodeId.ShouldBe(endNodeId);
        graphEdge.Variant.ShouldBe(EdgeType.Direct);
        graphEdge.Length.ShouldBe(5.0, tolerance: 1e-6); // 3-4-5 right triangle
        graphEdge.VariantParameters.ShouldBeEmpty();
    }

    [TestMethod]
    public void BuildFromNurbs_Degree1MultiSegment_ReturnsPolypath()
    {
        // A degree-1 NURBS with 4 control points is a 3-segment polyline → Polypath with 2 inner points.
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var nurbs = new Nurbs
        {
            Degree = 1,
            KnotVector = [0.0, 0.0, 0.25, 0.5, 0.75, 1.0, 1.0],
            ControlPoints =
            [
                new NurbsControlPoint(0.0, 0.0), // P0 (start)
                new NurbsControlPoint(1.0, 0.0), // P1
                new NurbsControlPoint(2.0, 0.0), // P2
                new NurbsControlPoint(3.0, 0.0)  // P3 (end)
            ]
        };

        var graphEdge = _builder.BuildFromNurbs(nurbs, startNodeId, endNodeId);

        graphEdge.FromNodeId.ShouldBe(startNodeId);
        graphEdge.ToNodeId.ShouldBe(endNodeId);
        graphEdge.Variant.ShouldBe(EdgeType.Polypath);
        graphEdge.Length.ShouldBe(3.0, tolerance: 1e-6); // total horizontal distance
        graphEdge.VariantParameters.Count.ShouldBe(2);   // P1 and P2 as inner waypoints
        graphEdge.VariantParameters.First().X!.Value.ShouldBe(1.0, tolerance: 1e-6);
        graphEdge.VariantParameters.First().Y!.Value.ShouldBe(0.0, tolerance: 1e-6);
        graphEdge.VariantParameters.Last().X!.Value.ShouldBe(2.0, tolerance: 1e-6);
        graphEdge.VariantParameters.Last().Y!.Value.ShouldBe(0.0, tolerance: 1e-6);
    }
}
