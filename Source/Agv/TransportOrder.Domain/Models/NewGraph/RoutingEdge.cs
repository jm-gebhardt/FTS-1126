namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

public class RoutingEdge : RoutingResource
{
    public RoutingNode StartNode { get; } = null!;

    public RoutingNode EndNode { get; } = null!;

    public bool RotationAllowed { get; }

    public double? MaxSpeed { get; }

    public double? MaxRotationSpeed { get; }

    public Trajectory? Trajectory { get; } = null!;

    /// <summary>
    /// Length in meters
    /// </summary>
    public double Length { get; }  //decide which length is used for rotations

    //public double DrivingTime { get; }

    // actions? -> feedback from Andreas
}