namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Trigger;

/// <summary>
/// Publishes trigger related signal-R messages.
/// </summary>
public interface ITriggerPublisher
{
    /// <summary>
    /// Publishes the change update of a trigger.
    /// </summary>
    public Task SendTriggerChangedAsync(Domain.Models.Trigger.Trigger trigger);

    /// <summary>
    /// Publishes the deletion of a trigger.
    /// </summary>
    public Task SendTriggerDeletedAsync(Guid triggerId);
}