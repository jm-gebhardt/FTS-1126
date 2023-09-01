using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

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
    public Task AddActionGroupAsync(Common.Messages.Actions.ActionGroup actionGroup, CancellationToken cancellationToken);

    /// <summary>
    /// Update ActionState and unlock Agv
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the action</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [UsedImplicitly]
    public Task CompleteActionAsync(Guid correlationId, CancellationToken cancellationToken);

}