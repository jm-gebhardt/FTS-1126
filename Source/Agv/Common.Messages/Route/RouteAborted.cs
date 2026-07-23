using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Route;

/// <summary>
/// Represents the event raised when a route is aborted.
/// </summary>
/// <param name="RouteId">The unique identifier of the route.</param>
/// <param name="AgvCorrelationId">The correlation identifier of the AGV.</param>
[PublicAPI]
public record RouteAborted(Guid RouteId, Guid AgvCorrelationId) : CorrelatedBy<Guid>
{
    /// <summary>
    /// The correlation identifier for Masstransit
    /// </summary>
    public Guid CorrelationId => RouteId;
}