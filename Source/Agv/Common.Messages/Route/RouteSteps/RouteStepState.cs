using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Route.RouteSteps;

/// <summary>
/// Represents the state of a route step.
/// </summary>
/// <param name="RouteStepId">The unique identifier of the route step.</param>
/// <param name="Status">The status of the route step.</param>
/// <param name="FailureReason">The reason for the failure, if any.</param>
[PublicAPI]
public record RouteStepState(Guid RouteStepId, RouteStepStatus Status, string? FailureReason = null);