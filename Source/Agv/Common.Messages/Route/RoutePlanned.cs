
using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Route;

/// <summary>
/// Represents the event raised when a route is planned for an AGV.
/// </summary>
/// <param name="RouteId">The unique identifier of the route.</param>
/// <param name="AgvCorrelationId">The correlation identifier of the AGV.</param>
/// <param name="RouteSteps">The steps of the planned route.</param>
[PublicAPI]
public record RoutePlanned(Guid RouteId, Guid AgvCorrelationId, RouteStep[] RouteSteps)
{
    /// <summary>
    /// The correlation identifier for Masstransit
    /// </summary>
    public Guid CorrelationId => RouteId;
}