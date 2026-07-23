using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Route;

/// <summary>
/// Represents the event raised when route steps are released and should be added to base.
/// </summary>
/// <param name="RouteId">The unique identifier of the route.</param>
/// <param name="AgvCorrelationId">The correlation identifier of the AGV.</param>
/// <param name="RouteStepIds">The identifiers of the released route steps.</param>
[PublicAPI]
public record RouteStepsReleased(Guid RouteId, Guid AgvCorrelationId, Guid[] RouteStepIds) : CorrelatedBy<Guid>
{
    /// <summary>
    /// The correlation identifier for Masstransit
    /// </summary>
    public Guid CorrelationId => RouteId;
}