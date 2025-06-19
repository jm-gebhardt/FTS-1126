namespace Gebhardt.Acc.KaeserSidecar.Domain.Services;

/// <summary>
/// A handler for short loop waiting.
/// </summary>
public interface IWaitingHandler
{
    /// <summary>
    /// Handle waiting for the given action and agv.
    /// </summary>
    /// <param name="actionCorrelationId">The correlation id of the action that needs target selection.</param>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="orderCreationTime">The time the order was created.</param>
    Task StartHandling(Guid actionCorrelationId, Guid agvCorrelationId, DateTime orderCreationTime);

    /// <summary>
    /// Stop handling waiting of an action.
    /// </summary>
    Task StopHandling(Guid actionCorrelationId);
}