using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// The current route of the Agv.
/// </summary>
public record CurrentAgvRouteDto
{
    /// <summary>
    /// The CorrelationId of the Agv.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// The current route of an Agv.
    /// </summary>
    public CurrentRouteDto CurrentRoute { get; init; }

    /// <inheritdoc cref="CurrentRouteDto"/>>
    public CurrentAgvRouteDto(Guid correlationId, CurrentRouteDto currentRoute)
    {
        CorrelationId = correlationId;
        CurrentRoute = currentRoute;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    [PublicAPI]
    private CurrentAgvRouteDto()
    {
        CurrentRoute = null!;
    }
}