namespace Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// An action handled at the waiting point.
/// </summary>
public class WaitingPointAction
{
    /// <summary>
    /// The correlation id of the action.
    /// </summary>
    public Guid AgvActionCorrelationId { get; init; }

    /// <summary>
    /// The correlation id of the agv.
    /// </summary>
    public Guid AgvCorrelationId { get; init; }

    /// <summary>
    /// The target station.
    /// </summary>
    public Guid? TargetNodeId { get; private set; }

    /// <summary>
    /// The time the order was created.
    /// </summary>
    public DateTime CreatedTime { get; init; }

    /// <inheritdoc cref="WaitingPointAction"/>>
    public WaitingPointAction(Guid agvActionCorrelationId, Guid agvCorrelationId, Guid? targetNodeId, DateTime createdTime)
    {
        AgvActionCorrelationId = agvActionCorrelationId;
        AgvCorrelationId = agvCorrelationId;
        TargetNodeId = targetNodeId;
        CreatedTime = createdTime;
    }

    /// <inheritdoc cref="WaitingPointAction"/>>
    public WaitingPointAction(Guid agvActionCorrelationId, Guid agvCorrelationId, DateTime createdTime)
    {
        AgvActionCorrelationId = agvActionCorrelationId;
        AgvCorrelationId = agvCorrelationId;
        CreatedTime = createdTime;
    }

    /// <summary>
    /// The target of the action.
    /// </summary>
    /// <param name="target"></param>
    public void SetTarget(Guid target)
    {
        TargetNodeId = target;
    }
}