namespace Gebhardt.Acc.Common.Messages.ActionGroups.Events;

/// <summary>
/// Marker interface for agv actions.
/// </summary>
public interface IAgvActionEvent
{
    /// <summary>
    /// The correlation id of the action.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// The correlation id of the Agv.
    /// </summary>
    Guid AgvCorrelationId { get; }
}