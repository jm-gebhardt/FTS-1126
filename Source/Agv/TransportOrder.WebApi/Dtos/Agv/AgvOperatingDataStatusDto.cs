using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Dto for operational data of an AGV.
/// </summary>
[PublicAPI]
public record AgvOperatingDataStatusDto : DatedMessage
{
    /// <summary>The CorrelationId of the Agv</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>Total distance driven by AGV since commissioning [km]</summary>
    public float TotalDistance { get; init; }

    /// <summary>Total lifetime driven by AGV since commissioning [h]</summary>
    public float TotalLifetime { get; init; }

    /// <inheritdoc cref="AgvOperatingDataStatusDto"/>
    /// <param name="correlationId">The CorrelationId of the Agv</param>
    /// <param name="totalDistance">Total distance driven by AGV since commissioning [km].</param>
    /// <param name="totalLifetime">Total lifetime driven by AGV since commissioning [hm].</param>
    public AgvOperatingDataStatusDto(
        [PublicAPI] Guid correlationId,
        float totalDistance,
        float totalLifetime
    )
    {
        CorrelationId = correlationId;
        TotalDistance = totalDistance;
        TotalLifetime = totalLifetime;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    public AgvOperatingDataStatusDto()
    {
        TotalDistance = 0;
        TotalLifetime = 0;
    }
}