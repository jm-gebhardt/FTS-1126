using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// The current route of the Agv. It is null when the Agv has completed the last action.
/// </summary>
public record AgvRouteDto
{
    /// <summary>The remaining route in the form of nodes and edges.</summary>
    /// <example>N002-001,N003-001,N003-002,N004-002,D2</example>
    /// <example>D2</example>
    public string[] RemainingRoute { get; init; }

    /// <inheritdoc cref="AgvRouteDto"/>>
    public AgvRouteDto(string[] remainingRoute)
    {
        RemainingRoute = remainingRoute;
    }

    /// <summary>
    /// Deserializer constructor
    /// </summary>
    [PublicAPI]
    private AgvRouteDto()
    {
        RemainingRoute = null!;
    }
}