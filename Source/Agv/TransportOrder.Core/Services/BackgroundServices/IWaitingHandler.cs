using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

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

    /// <summary>
    /// Get all primary and secondary configured targets.
    /// </summary>
    /// <returns></returns>
    [PublicAPI]
    Task<ICollection<Guid>> GetConfiguredTargets();

    /// <summary>
    /// Configure primary and secondary targets.
    /// </summary>
    /// <param name="primaryTarget">The primary target for the waiting point.</param>
    /// <param name="secondaryTargets">Optional secondary targets.</param>
    /// <returns></returns>
    Task ConfigureTargets(Guid primaryTarget, ICollection<Guid> secondaryTargets);
}