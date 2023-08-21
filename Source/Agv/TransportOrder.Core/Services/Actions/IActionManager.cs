using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <summary>
/// Action Manager
/// </summary>
public interface IActionManager
{
    /// <summary>
    /// Save ActionGroup and try to assign it to an Agv
    /// </summary>
    /// <param name="actionGroup"></param>
    /// <param name="cancellationToken"></param>
    public Task AddActionGroupAsync(ActionGroup actionGroup, CancellationToken cancellationToken);

    /// <summary>
    /// Update ActionState and unlock Agv
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the action</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [UsedImplicitly]
    public Task CompleteActionAsync(Guid correlationId, CancellationToken cancellationToken);

    /// <summary>
    /// External trigger for assigning a new ActionGroup to an Agv in case the Agv is ready to transition to busy state (check is done internally)
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <param name="cancellationToken"></param>
    public Task AssignExclusivelyIfAgvReadyAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if it makes sense to assign a new action to any charging Agv
    /// </summary>
    public Task CheckChargingAgvs(CancellationToken cancellationToken);
}