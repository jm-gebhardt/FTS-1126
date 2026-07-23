using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Route;

/// <summary>
/// Represents the event raised when a route is updated for an AGV.
/// </summary>
/// <param name="RouteId">The unique identifier of the route.</param>
/// <param name="AgvCorrelationId">The correlation identifier of the AGV.</param>
/// <param name="RouteSteps">The steps of the updated route.</param>
[PublicAPI]
public record RouteUpdated(Guid RouteId, Guid AgvCorrelationId, RouteStep[] RouteSteps) : CorrelatedBy<Guid>
{
    /// <summary>
    /// The correlation identifier for Masstransit
    /// </summary>
    public Guid CorrelationId => RouteId;
}