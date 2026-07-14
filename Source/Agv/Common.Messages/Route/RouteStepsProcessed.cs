
using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Route;

/// <summary>
/// Represents the event raised when route steps are processed by the AGV.
/// </summary>
/// <param name="RouteId">The unique identifier of the route.</param>
/// <param name="AgvCorrelationId">The correlation identifier of the AGV.</param>
/// <param name="RouteStepStates">The states of the processed route steps.</param>
[PublicAPI]
public record RouteStepsProcessed(Guid RouteId, Guid AgvCorrelationId, RouteStepState[] RouteStepStates)
{
    /// <summary>
    /// The correlation identifier for Masstransit
    /// </summary>
    public Guid CorrelationId => RouteId;
}