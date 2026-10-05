using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using ControlPoint = Gebhardt.Acc.Common.Domain.Models.NewGraph.ControlPoint;
using Trajectory = Gebhardt.Acc.Common.Domain.Models.NewGraph.Trajectory;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.LIF.LifToGraphConversion;

internal class VehicleTypeEdgePropertyHelper
{
    public VehicleTypeEdgeProperty EdgeProperty { get; }

    public Trajectory? Trajectory { get; }

    public double? EdgeStartTheta { get; }

    public double? EdgeEndTheta { get; }

    public VehicleTypeEdgePropertyHelper(VehicleTypeEdgeProperty edgeProperty)
    {
        EdgeProperty = edgeProperty;
        Trajectory = ConvertTrajectory(edgeProperty.Trajectory);

        (EdgeStartTheta, EdgeEndTheta) = CalculateEdgeThetas(edgeProperty, Trajectory);
    }

    private static Trajectory? ConvertTrajectory(Lif.Trajectory? trajectory)
    {
        if (trajectory is null)
        {
            return null;
        }

        var controlPoints = trajectory.ControlPoints.Select(p => new ControlPoint(p.X, p.Y, p.Weight)).ToList();

        return new Trajectory(controlPoints, trajectory.KnotVector.ToArray(), trajectory.Degree);
    }

    private static (double? EdgeStartTheta, double? EdgeEndTheta) CalculateEdgeThetas(VehicleTypeEdgeProperty edgeProperty, Trajectory? trajectory)
    {
        if (trajectory is null || edgeProperty.VehicleOrientation is not { } vehicleOrientation)
        {
            return (null, null);
        }

        bool isGlobal = string.Equals(edgeProperty.OrientationType, "GLOBAL", StringComparison.OrdinalIgnoreCase);

        if (isGlobal)
        {
            return (vehicleOrientation, vehicleOrientation);
        }

        double trajectoryStartAngle = trajectory.CalculateStartAngle();
        double trajectoryEndAngle = trajectory.CalculateEndAngle();

        double edgeStartTheta = Mathematics.NormalizeAngle(trajectoryStartAngle + vehicleOrientation);
        double edgeEndTheta = Mathematics.NormalizeAngle(trajectoryEndAngle + vehicleOrientation);

        return (edgeStartTheta, edgeEndTheta);
    }
}