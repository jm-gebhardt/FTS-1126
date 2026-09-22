using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Reservations;

/// <summary>
/// Represents a reservation ticket for a routing resource.
/// </summary>
public record ReservationTicket
{
    /// <summary>
    /// Unique identifier of the reservation ticket.
    /// </summary>
    public Guid TicketId { get; }

    /// <summary>
    /// The routing resource associated with the reservation ticket.
    /// </summary>
    public RoutingResource RoutingResource { get; }
    
    /// <summary>
    /// The AGV correlation identifier associated with the reservation ticket.
    /// </summary>
    public Guid AgvCorrelationId { get; }

    internal ReservationTicket(Guid TicketId, RoutingResource RoutingResource, Guid AgvCorrelationId)
    {
        this.TicketId = TicketId;
        this.RoutingResource = RoutingResource;
        this.AgvCorrelationId = AgvCorrelationId;
    }
}

