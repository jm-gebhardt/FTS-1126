using TransportOrder.Domain.Models.ActionGroup;

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
    public Task AddActionGroupAsync(ActionGroup actionGroup, CancellationToken cancellationToken);
}