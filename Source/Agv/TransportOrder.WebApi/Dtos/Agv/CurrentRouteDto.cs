using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// The current route of the Agv. It is null when the Agv has completed the last action.
/// </summary>
public record CurrentRouteDto : DatedMessage
{
    /// <summary>The remaining route in the form of nodes and edges.</summary>
    /// <example>N002-001,N003-001,N003-002,N004-002,D2</example>
    /// <example>D2</example>
    public string[] RemainingRoute { get; init; }

    /// <inheritdoc cref="CurrentRouteDto"/>>
    public CurrentRouteDto(string[] remainingRoute)
    {
        RemainingRoute = remainingRoute;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    [PublicAPI]
    private CurrentRouteDto()
    {
        RemainingRoute = null!;
    }
}