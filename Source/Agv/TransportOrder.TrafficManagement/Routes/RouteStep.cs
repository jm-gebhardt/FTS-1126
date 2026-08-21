using System.Diagnostics;
using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Reservations;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Routes;

/// <summary>
/// Represents a single step in a route, which corresponds to traversing a routing edge.
/// </summary>
internal class RouteStep(RoutingEdge routingEdge, double drivingTime, double cost)
{
    private readonly List<ReservationTicket> _reservations = [];

    /// <summary>
    /// Unique identifier for the route step.
    /// </summary>
    public Guid RouteStepId { get; } = Guid.NewGuid();

    /// <summary>
    /// The routing edge that this step corresponds to.
    /// </summary>
    public RoutingEdge RoutingEdge { get; } = routingEdge;

    /// <summary>
    /// The length of the routing edge.
    /// </summary>
    public double Length => RoutingEdge.Length;
    
    /// <summary>
    /// The driving time required to traverse the routing edge.
    /// </summary>
    public double DrivingTime { get; } = drivingTime;

    /// <summary>
    /// The cost associated with traversing the routing edge.
    /// </summary>
    public double Cost { get; } = cost;

    /// <summary>
    /// The current status of the route step.
    /// </summary>
    public RouteStepStatus Status { get; private set; } = RouteStepStatus.Planned;

    /// <summary>
    /// Indicates whether the route step is completed.
    /// </summary>
    public bool IsCompleted => Status is RouteStepStatus.Completed;

    /// <summary>
    /// Indicates whether the route step is planned.
    /// </summary>
    public bool IsPlanned => Status is RouteStepStatus.Planned;

    /// <summary>
    /// The reservations associated with this route step.
    /// </summary>
    public IReadOnlyCollection<ReservationTicket> Reservations => _reservations;

    public void SetReleased()
    {
        Debug.Assert(Status is RouteStepStatus.Planned);
        Status = RouteStepStatus.Released;
    }

    public void SetAcknowledged()
    {
        Debug.Assert(Status is RouteStepStatus.Released);
        Status = RouteStepStatus.Acknowledged;
    }

    public void SetCompleted()
    {
        Debug.Assert(Status is RouteStepStatus.Released or RouteStepStatus.Acknowledged);
        Status = RouteStepStatus.Completed;
    }

    public void SetFailed()
    {
        Debug.Assert(Status is not RouteStepStatus.Completed);
        Status = RouteStepStatus.Failed;
    }

    public void AddReservations(IEnumerable<ReservationTicket> reservationTickets)
    {
        _reservations.AddRange(reservationTickets);
    }

    public void ClearReservations()
    {
        _reservations.Clear();
    }

    public override string ToString()
    {
        return $"{GetStatusPrefix()} {RoutingEdge.StartNode.Id} -> {RoutingEdge.EndNode.Id}";
    }

    private string GetStatusPrefix()
    {
        return Status switch
        {
            RouteStepStatus.Planned => "<<",
            RouteStepStatus.Released => "##",
            RouteStepStatus.Acknowledged => "##",
            RouteStepStatus.Completed => "**",
            RouteStepStatus.Failed => "FF",
            _ => "??"
        };
    }
}