namespace Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// An action handled at the waiting point.
/// </summary>
public class WaitingPointAction
{
    /// <summary>
    /// The correlation id of the action.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The correlation id of the agv.
    /// </summary>
    public Guid AgvCorrelationId { get; init; }

    /// <summary>
    /// The target station.
    /// </summary>
    public Guid? Target { get; private set; }

    /// <summary>
    /// The time the order was created.
    /// </summary>
    public DateTime CreatedTime { get; init; }

    /// <inheritdoc cref="WaitingPointAction"/>>
    public WaitingPointAction(Guid correlationId, Guid agvCorrelationId, Guid? target, DateTime createdTime)
    {
        CorrelationId = correlationId;
        AgvCorrelationId = agvCorrelationId;
        Target = target;
        CreatedTime = createdTime;
    }

    /// <inheritdoc cref="WaitingPointAction"/>>
    public WaitingPointAction(Guid correlationId, Guid agvCorrelationId, DateTime createdTime)
    {
        CorrelationId = correlationId;
        AgvCorrelationId = agvCorrelationId;
        CreatedTime = createdTime;
    }

    /// <summary>
    /// The target of the action.
    /// </summary>
    /// <param name="target"></param>
    public void SetTarget(Guid target)
    {
        Target = target;
    }
}