using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;

/// <summary>
/// Per-step progress derived from an order state report. Plain domain result — the caller decides whether and
/// how to publish this (e.g. wrapped in a <c>RouteStepsProcessed</c> event).
/// </summary>
/// <param name="RouteId">The unique identifier of the route.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
/// <param name="ChangedSteps">The steps whose status changed as a result of this report.</param>
/// <param name="OrderCompleted">True if this report caused the current orderId's steps to become fully completed.</param>
[PublicAPI]
public sealed record RouteStepProgress(Guid RouteId, Guid AgvCorrelationId, ICollection<RouteStepState> ChangedSteps, bool OrderCompleted);
