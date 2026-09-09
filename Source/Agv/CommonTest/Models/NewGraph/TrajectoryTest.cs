using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Models.NewGraph;

[TestClass]
public class TrajectoryTest
{
    [TestMethod]
    public void Sample_StraightLine_ReturnsPointsOnLine()
    {
        var trajectory = CreateStraightLine();

        IReadOnlyList<PolygonPoint> samples = trajectory.Sample(5);

        samples.Count.ShouldBe(5);
        samples[0].X.ShouldBe(0.0, Tolerance);
        samples[0].Y.ShouldBe(0.0, Tolerance);
        samples[2].X.ShouldBe(5.0, Tolerance);
        samples[2].Y.ShouldBe(0.0, Tolerance);
        samples[^1].X.ShouldBe(10.0, Tolerance);
        samples[^1].Y.ShouldBe(0.0, Tolerance);
    }

    [TestMethod]
    public void CalculateLength_StraightLine_ReturnsDistanceBetweenControlPoints()
    {
        var trajectory = CreateStraightLine();

        trajectory.CalculateLength(40).ShouldBe(10.0, Tolerance);
    }

    [TestMethod]
    public void CalculateLength_QuarterCircle_ApproximatesArcLength()
    {
        var trajectory = CreateQuarterCircle();

        // Arc length of a quarter circle with radius 1 is PI / 2.
        trajectory.CalculateLength(500).ShouldBe(Math.PI / 2.0, 1e-3);
    }

    [TestMethod]
    public void CalculateStartAngleAndEndAngle_QuarterCircle_ReturnsTangentOrientations()
    {
        var trajectory = CreateQuarterCircle();

        // The quarter circle starts at (1, 0) heading in +Y direction and ends at (0, 1) heading in -X direction.
        trajectory.CalculateStartAngle().ShouldBe(Math.PI / 2.0, Tolerance);
        trajectory.CalculateEndAngle().ShouldBe(Math.PI, Tolerance);
    }

    [TestMethod]
    public void CalculateStartAngle_StraightLineBackwards_ReturnsPi()
    {
        var trajectory = new Trajectory(
            [new ControlPoint(10.0, 0.0), new ControlPoint(0.0, 0.0)],
            [0.0, 0.0, 1.0, 1.0],
            1);

        Math.Abs(trajectory.CalculateStartAngle()).ShouldBe(Math.PI, Tolerance);
    }

    private const double Tolerance = 1e-6;

    private static Trajectory CreateQuarterCircle() =>
        new(
            [
                new ControlPoint(1.0, 0.0),
                new ControlPoint(1.0, 1.0, Math.Sqrt(2.0) / 2.0),
                new ControlPoint(0.0, 1.0)
            ],
            [0.0, 0.0, 0.0, 1.0, 1.0, 1.0],
            2);

    private static Trajectory CreateStraightLine() =>
        new([new ControlPoint(0.0, 0.0), new ControlPoint(10.0, 0.0)], [0.0, 0.0, 1.0, 1.0], 1);
}
