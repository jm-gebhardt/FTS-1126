using System.Diagnostics.CodeAnalysis;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Reservations;

/// <summary>
/// Manages reservations of routing resources for AGVs, ensuring that resources are not reserved by multiple AGVs simultaneously.
/// </summary>
public class ReservationManager(ILogger<ReservationManager> logger)
{
    private readonly Dictionary<Guid, List<ReservationTicket>> _reservations = new();

    /// <summary>
    /// Attempts to reserve a routing resource for a specific AGV. If the resource is already reserved by another AGV, the reservation will fail and return false. If successful, it returns true and provides a ReservationTicket that can be used to unreserve the resource later.
    /// </summary>
    /// <param name="routingResource">The routing resource to be reserved.</param>
    /// <param name="agvCorrelationId">The unique identifier of the AGV attempting the reservation.</param>
    /// <param name="ticket">The reservation ticket if the reservation is successful.</param>
    /// <returns>True if the reservation is successful; otherwise, false.</returns>
    public bool TryReserve(RoutingResource routingResource, Guid agvCorrelationId, [NotNullWhen(true)] out ReservationTicket? ticket)
    {
        if (IsBlockedByOtherAgv(routingResource, agvCorrelationId))
        {
            ticket = null;
            return false;
        }

        ticket = Reserve(routingResource, agvCorrelationId);
        return true;
    }

    /// <summary>
    /// Attempts to reserve multiple routing resources for a specific AGV. The resources are only reserved if all of them are available.
    /// </summary>
    /// <param name="routingResources">The routing resources to be reserved.</param>
    /// <param name="agvCorrelationId">The unique identifier of the AGV attempting the reservation.</param>
    /// <param name="tickets">The reservation tickets if all reservations are successful.</param>
    /// <returns>True if all reservations are successful; otherwise, false.</returns>
    public bool TryReserve(IReadOnlyCollection<RoutingResource> routingResources, Guid agvCorrelationId, out IReadOnlyCollection<ReservationTicket> tickets)
    {
        if (routingResources.Any(r => IsBlockedByOtherAgv(r, agvCorrelationId)))
        {
            tickets = [];
            return false;
        }

        tickets = routingResources.Select(r => Reserve(r, agvCorrelationId)).ToList();
        return true;
    }

    /// <summary>
    /// Unreserves a previously reserved routing resource using the provided reservation ticket.
    /// </summary>
    public void Unreserve(ReservationTicket ticket)
    {
        bool wasRemoved = false;

        if (_reservations.TryGetValue(ticket.RoutingResource.Id, out var tickets))
        {
            wasRemoved = tickets.Remove(ticket);

            if (tickets.Count == 0)
            {
                _reservations.Remove(ticket.RoutingResource.Id);
            }
        }

        if (!wasRemoved)
        {
            logger.LogWarning("Failed to unreserve ticket {TicketId} (RoutingResourceId: {RoutingResourceId}, AgvCorrelationId: {AgvCorrelationId})", ticket.TicketId, ticket.RoutingResource.Id, ticket.AgvCorrelationId);
        }
    }

    private bool IsBlockedByOtherAgv(RoutingResource routingResource, Guid agvCorrelationId)
    {
        IEnumerable<RoutingResource> relevantResources = [routingResource, .. routingResource.ConflictingResources];

        foreach (var relevantResource in relevantResources)
        {
            if (_reservations.TryGetValue(relevantResource.Id, out var tickets))
            {
                if (tickets.Any(t => t.AgvCorrelationId != agvCorrelationId))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private ReservationTicket Reserve(RoutingResource routingResource, Guid agvCorrelationId)
    {
        var ticket = new ReservationTicket(Guid.NewGuid(), routingResource, agvCorrelationId);

        if (_reservations.TryGetValue(routingResource.Id, out var tickets))
        {
            tickets.Add(ticket);
        }
        else
        {
            _reservations[routingResource.Id] = [ticket];
        }

        return ticket;
    }
}