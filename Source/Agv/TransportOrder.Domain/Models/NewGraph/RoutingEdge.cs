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
    public double Length { get; }  //does length exist without trajectory?, decide which length is used for rotations

    //public double DrivingTime { get; } // create method to calculate driving time based on length and max speed, maybe also consider acceleration and deceleration

    // actions? -> feedback from Andreas
}