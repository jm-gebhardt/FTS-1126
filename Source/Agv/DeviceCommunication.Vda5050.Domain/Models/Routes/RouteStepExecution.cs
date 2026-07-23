using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using JetBrains.Annotations;
using AgvAction = Gebhardt.Acc.Common.Messages.Route.AgvActions.AgvAction;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Routes;

/// <summary>
/// Durable execution state of a single <see cref="Common.Messages.Route.RouteSteps.RouteStep"/> within a <see cref="RouteExecution"/>.
/// </summary>
[PublicAPI]
public class RouteStepExecution
{
    /// <summary>
    /// The unique identifier of the route step, as assigned by traffic management.
    /// </summary>
    public Guid RouteStepId { get; init; }

    /// <summary>
    /// The identifier of the start node.
    /// </summary>
    public Guid StartNodeId { get; init; }

    /// <summary>
    /// The identifier of the edge. Null if this step does not involve travel over an edge.
    /// </summary>
    public Guid? EdgeId { get; init; }

    /// <summary>
    /// The identifier of the end node.
    /// </summary>
    public Guid EndNodeId { get; init; }

    /// <summary>
    /// The action to be performed at the end node, if any.
    /// </summary>
    public AgvAction? ActionAtEndNode { get; init; }

    /// <summary>
    /// True, if this step is a stationary-action step (no edge, start and end node are the same position) that
    /// merges its action onto the node already created by the preceding travel step, instead of a new position.
    /// </summary>
    public bool IsStationaryAction => EdgeId == null && StartNodeId == EndNodeId;

    /// <summary>
    /// True, if this step has been released into the base.
    /// </summary>
    public bool Released { get; set; }

    /// <summary>
    /// The current status of the step, as last reported by the AGV.
    /// </summary>
    public RouteStepStatus Status { get; set; } = RouteStepStatus.Planned;

    /// <summary>
    /// The reason for the failure, if <see cref="Status"/> is <see cref="RouteStepStatus.Failed"/>.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// The VDA5050 orderId this step was released under. Kept alongside the sequence ids below so that
    /// state attribution survives an orderId rotation on the owning <see cref="RouteExecution"/>.
    /// </summary>
    public Guid? CurrentOrderId { get; set; }

    /// <summary>
    /// The VDA5050 sequence id of the node produced for this step, once translated onto an order.
    /// </summary>
    public int? NodeSequenceId { get; set; }

    /// <summary>
    /// The VDA5050 sequence id of the edge produced for this step, once translated onto an order.
    /// Null for stationary-action steps, which share their node's sequence id instead.
    /// </summary>
    public int? EdgeSequenceId { get; set; }

    /// <summary>
    /// The wire actionId chosen when this step's action was translated onto an order node, used to attribute
    /// a later <c>actionStates</c> entry back to this 
    /// </summary>
    public string? VdaActionId { get; set; }

    /// <inheritdoc cref="RouteStepExecution"/>
    public RouteStepExecution(Guid routeStepId, Guid startNodeId, Guid? edgeId, Guid endNodeId, AgvAction? actionAtEndNode)
    {
        RouteStepId = routeStepId;
        StartNodeId = startNodeId;
        EdgeId = edgeId;
        EndNodeId = endNodeId;
        ActionAtEndNode = actionAtEndNode;
    }

    /// <summary>
    /// Release this step into the base.
    /// </summary>
    public void Release()
    {
        Released = true;
    }

    /// <summary>
    /// Record the status reported by the AGV for this 
    /// </summary>
    /// <param name="status">The reported status.</param>
    /// <param name="failureReason">The failure reason, if any.</param>
    public void RecordStatus(RouteStepStatus status, string? failureReason = null)
    {
        Status = status;
        FailureReason = failureReason;
    }

    /// <summary>
    /// True, if the execution has reached the given node.
    /// </summary>
    /// <param name="nodeSequenceId">Sequence id of the node.</param>
    public bool HasReachedNode(int nodeSequenceId)
    {
        return NodeSequenceId.HasValue && nodeSequenceId >= NodeSequenceId.Value;
    }

    /// <summary>
    /// Determine the current <see cref="RouteStepStatus"/>
    /// </summary>
    /// <param name="orderState">The current order state.</param>
    public RouteStepStatus DetermineStatus(OrderState orderState)
    {
        bool attributedError = VdaActionId != null && orderState.Errors.Any(e => e.ErrorReferences?.Any(r => r.ReferenceValue == VdaActionId) == true);
        if (attributedError)
        {
            return RouteStepStatus.Failed;
        }

        if (ActionAtEndNode != null)
        {
            var actionState = VdaActionId != null ? orderState.ActionStates.SingleOrDefault(a => a.ActionId == VdaActionId) : null;
            if (actionState == null)
            {
                return HasReachedNode(orderState.LastNodeSequenceId) ? RouteStepStatus.Started : RouteStepStatus.Planned;
            }
            return actionState.ActionStatus switch
            {
                VehicleActionStatus.Finished => RouteStepStatus.Completed,
                VehicleActionStatus.Failed => RouteStepStatus.Failed,
                VehicleActionStatus.Running => RouteStepStatus.Started,
                _ => Released ? RouteStepStatus.Acknowledged : RouteStepStatus.Planned
            };
        }

        if (!NodeSequenceId.HasValue)
        {
            return Status;
        }
        if (HasReachedNode(orderState.LastNodeSequenceId))
        {
            return RouteStepStatus.Completed;
        }
        return Released ? RouteStepStatus.Acknowledged : RouteStepStatus.Planned;
    }

    /// <summary>
    /// Get the failure reason for this route step.
    /// </summary>
    /// <param name="orderState">The order state.</param>
    public string? ExtractFailureReason(OrderState orderState)
    {
        var error = orderState.Errors.SingleOrDefault(e => e.ErrorReferences?.Any(r => r.ReferenceValue == VdaActionId) == true);
        return error?.ErrorDescription ?? error?.ErrorType;
    }
}
