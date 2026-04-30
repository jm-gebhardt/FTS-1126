using Gebhardt.Core.NotificationCenter.Contracts.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Notification.Models;

/// <summary>
/// A notification that can be sent out with a <see cref="INotificationManager"/>.
/// </summary>
/// <param name="Type">The type of notification.</param>
/// <param name="Title">The display title of the notification.</param>
/// <param name="Description">The description of the notification.</param>
/// <param name="TemplateValues">Template parameters and the values to replace them with.</param>
/// <param name="Links">Links for the notification.</param>
/// <param name="OccurrenceTimestamp">The timestamp that the event for this notification occured.</param>
[PublicAPI]
public record Notification(NotificationType Type, string Title, string Description, Dictionary<string, string> TemplateValues, IList<NotificationLink> Links, DateTime? OccurrenceTimestamp = null)
{
    internal NotificationDto ToDto()
    {
        IList<LinkDto> links = Links.Select(l => l.ToDto()).ToList();

        return new NotificationDto { Description = Description, NotificationType = Type.Name, Properties = TemplateValues, Title = Title, Links = links, EventRaised = OccurrenceTimestamp ?? DateTime.UtcNow};
    }
}