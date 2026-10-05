using Gebhardt.Acc.Common.Domain.Models.Errors;
using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Errors;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Routes;

/// <summary>
/// Aggregate root tracking the durable, cross-restart, cross-orderId-rotation execution state of a
/// route, from planning through release to completion or abort.
/// </summary>
[PublicAPI]
public class RouteExecution
{
    /// <summary>
    /// The unique identifier of the route, as assigned by traffic management.
    /// </summary>
    public Guid RouteId { get; init; }

    /// <summary>
    /// The correlation id of the assigned agv.
    /// </summary>
    public Guid AgvCorrelationId { get; init; }

    /// <summary>
    /// The live VDA5050 orderId for this route, or null before the first release.
    /// </summary>
    public Guid? CurrentOrderId { get; set; }

    /// <summary>
    /// True, once the AGV has been observed to have fully completed <see cref="CurrentOrderId"/>. Reset whenever
    /// a fresh orderId is minted. Distinct from <see cref="LifecycleStatus"/>: an order-cycle can complete while
    /// the route itself remains active and awaits further content via a later order-restart.
    /// </summary>
    public bool CurrentOrderCompleted { get; set; }

    /// <summary>
    /// Past orderIds used for this route, so that "an orderId is never reused" is enforceable and testable.
    /// </summary>
    public ICollection<Guid> OrderIdHistory { get; init; } = [];

    /// <summary>
    /// The ordered steps of the route, in traversal sequence.
    /// </summary>
    public ICollection<RouteStepExecution> Steps { get; init; } = [];

    /// <summary>
    /// The lifecycle status of the route.
    /// </summary>
    public RouteExecutionStatus LifecycleStatus { get; set; } = RouteExecutionStatus.Active;

    /// <inheritdoc cref="RouteExecution"/>
    public RouteExecution(Guid routeId, Guid agvCorrelationId)
    {
        RouteId = routeId;
        AgvCorrelationId = agvCorrelationId;
    }

    /// <summary>
    /// Apply an initial planned step sequence. All steps start unreleased.
    /// </summary>
    /// <param name="steps">The planned steps, in traversal sequence.</param>
    public void ApplyPlanned(ICollection<RouteStepExecution> steps)
    {
        ReplaceSteps(steps);
    }

    /// <summary>
    /// Apply an updated step sequence. Before any release has ever happened, this behaves like
    /// <see cref="ApplyPlanned"/> (full replace, all unreleased). Once an order is active, already-released
    /// steps that still exist in the updated sequence keep their released/status bookkeeping, so that
    /// the wire-level splice (done by the VDA5050 order-update mechanism against the AGV's live position)
    /// never has previously-committed base content silently un-released out from under it.
    /// </summary>
    /// <param name="steps">The updated steps, in traversal sequence.</param>
    public void ApplyUpdate(ICollection<RouteStepExecution> steps)
    {
        if (CurrentOrderId == null)
        {
            ReplaceSteps(steps);
            return;
        }
        MergeSteps(steps);
    }

    /// <summary>
    /// Release the given steps into the base, validating the base/horizon contiguity : after an unreleased step, no released step may follow in the sequence.
    /// </summary>
    /// <param name="routeStepIds">The identifiers of the steps to release.</param>
    /// <returns>Success, if the steps were released. Failure with a dedicated error otherwise.</returns>
    public Result ReleaseSteps(ICollection<Guid> routeStepIds)
    {
        List<RouteStepExecution> orderedSteps = [.. Steps];
        var knownStepIds = orderedSteps.Select(s => s.RouteStepId).ToHashSet();
        if (routeStepIds.Any(id => !knownStepIds.Contains(id)))
        {
            return RouteExecutionErrors.RouteStepNotFound;
        }

        HashSet<Guid> toRelease = [.. routeStepIds];
        bool contiguousSoFar = true;
        foreach (var step in orderedSteps)
        {
            bool willBeReleased = step.Released || toRelease.Contains(step.RouteStepId);
            if (willBeReleased && !contiguousSoFar)
            {
                return RouteExecutionErrors.ReleaseNotContiguous;
            }
            contiguousSoFar = willBeReleased;
        }

        foreach (var step in orderedSteps.Where(s => toRelease.Contains(s.RouteStepId)))
        {
            step.Release();
        }
        return Result.Success();
    }

    /// <summary>
    /// Mark the route as aborted. If an order was active, its cancellation is the caller's responsibility.
    /// </summary>
    public void Abort()
    {
        LifecycleStatus = RouteExecutionStatus.Aborted;
    }

    /// <summary>
    /// Mark the route as completed.
    /// </summary>
    public void Complete()
    {
        LifecycleStatus = RouteExecutionStatus.Completed;
    }

    /// <summary>
    /// Mint a fresh, previously-unused orderId for this route, recording the current one (if any) in
    /// <see cref="OrderIdHistory"/> and resetting <see cref="CurrentOrderCompleted"/>.
    /// </summary>
    /// <returns>The newly minted orderId.</returns>
    public Guid MintNextOrderId()
    {
        if (CurrentOrderId is { } previousOrderId)
        {
            OrderIdHistory.Add(previousOrderId);
        }
        CurrentOrderId = Guid.NewGuid();
        CurrentOrderCompleted = false;
        return CurrentOrderId.Value;
    }

    /// <summary>
    /// Record the status reported by the AGV for a single step.
    /// </summary>
    /// <param name="routeStepId">The identifier of the step.</param>
    /// <param name="status">The reported status.</param>
    /// <param name="failureReason">The failure reason, if any.</param>
    public void RecordStepStatus(Guid routeStepId, RouteStepStatus status, string? failureReason = null)
    {
        Steps.SingleOrDefault(s => s.RouteStepId == routeStepId)?.RecordStatus(status, failureReason);
    }

    private void ReplaceSteps(ICollection<RouteStepExecution> steps)
    {
        Steps.Clear();
        foreach (var step in steps)
        {
            step.Released = false;
            Steps.Add(step);
        }
    }

    private void MergeSteps(ICollection<RouteStepExecution> newSteps)
    {
        var existingById = Steps.ToDictionary(s => s.RouteStepId);
        Steps.Clear();
        foreach (var step in newSteps)
        {
            if (existingById.TryGetValue(step.RouteStepId, out var existing))
            {
                step.Released = existing.Released;
                step.Status = existing.Status;
                step.FailureReason = existing.FailureReason;
            }
            Steps.Add(step);
        }
    }
}

/// <summary>
/// The lifecycle status of a <see cref="RouteExecution"/>.
/// </summary>
[PublicAPI]
public enum RouteExecutionStatus
{
    /// <summary>The route is planned and/or in progress.</summary>
    Active,

    /// <summary>The route was aborted.</summary>
    Aborted,

    /// <summary>The route was completed.</summary>
    Completed
}
