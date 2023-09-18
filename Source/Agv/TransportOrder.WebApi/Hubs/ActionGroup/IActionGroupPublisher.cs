using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.ActionGroup;

/// <summary>
/// Publishes ActionGroup related Dtos
/// </summary>
public interface IActionGroupPublisher
{
    /// <summary>
    /// Publishes a newly created ActionGroup
    /// </summary>
    public Task SendActionGroupCreatedAsync(ActionGroupReadModel actionGroup);
    
    /// <summary>
    /// Publishes the change update of ActionGroup
    /// </summary>
    public Task SendActionGroupChangedAsync(ActionGroupReadModel actionGroup);
}