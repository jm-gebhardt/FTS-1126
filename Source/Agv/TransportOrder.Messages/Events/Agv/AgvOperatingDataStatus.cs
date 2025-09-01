using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Operating data status of an AGV.
/// </summary>
[PublicAPI]
public record AgvOperatingDataStatus
{
    /// <summary>The agv correlation id.</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>Total distance driven by AGV since commissioning [km].</summary>
    public float TotalDistance { get; init; }

    /// <summary>Total lifetime driven by AGV since commissioning [h].</summary>
    public float TotalLifetime { get; init; }

    /// <summary>
    /// AutoMapper
    /// </summary>
    public AgvOperatingDataStatus()
    {
    }

    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="totalDistance">Total distance driven by AGV since commissioning [km].</param>
    /// <param name="totalLifetime">Total lifetime driven by AGV since commissioning [h].</param>
    public AgvOperatingDataStatus(
        Guid correlationId,
        float totalDistance,
        float totalLifetime
    )
    {
        CorrelationId = correlationId;
        TotalDistance = totalDistance;
        TotalLifetime = totalLifetime;
    }

    /// <param name="agvStatus">The AgvStatus.</param>
    public AgvOperatingDataStatus(AgvStatus agvStatus) : this(
        agvStatus.CorrelationId,
        agvStatus.TotalDistance,
        agvStatus.TotalLifetime
    )
    {
    }
}