using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Action Manager
/// </summary>
public interface IActionManager
{
    /// <summary>
    /// Save ActionGroup and try to assign it to an Agv.
    /// Throws if the ActionGroup is manual and is not valid.
    /// </summary>
    /// <param name="actionGroup">The ActionGroup to add to the system</param>
    /// <param name="throwOnInvalid">If true the ActionGroup won't get added to the system and an exception will be thrown</param>
    /// <param name="cancellationToken"></param>
    public Task AddActionGroupAsync(ActionGroup actionGroup, bool throwOnInvalid, CancellationToken cancellationToken);
}