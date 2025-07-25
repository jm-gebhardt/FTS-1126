namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.RestrictedArea;

/// <summary>
/// Publishes restricted area related dtos
/// </summary>
public interface IRestrictedAreaPublisher
{
    /// <summary>
    /// Publishes a change of the restricted area.
    /// </summary>
    public Task SendRestrictedAreaChangedAsync(Domain.Models.RestrictedArea.RestrictedArea restrictedArea);

    /// <summary>
    /// Publishes deletion of a restricted area.
    /// </summary>
    public Task SendRestrictedAreaDeletedAsync(Guid id);
}