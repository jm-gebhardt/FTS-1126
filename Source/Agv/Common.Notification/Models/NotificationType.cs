using Gebhardt.Core.NotificationCenter.Contracts.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Notification.Models;

/// <summary>
/// A data transfer object wrapper for notification types.
/// </summary>
/// <param name="Name">The name of the notification type.</param>
[PublicAPI]
public record NotificationType(string Name)
{
    /// <summary>
    /// The name of this platform module.
    /// </summary>
    public const string PlatformModule = "ACC";

    /// <summary>
    /// Converts the notification type into its dto.
    /// </summary>
    internal NotificationTypeDto ToDto()
    {
        return new NotificationTypeDto { Application = PlatformModule, Title = Name };
    }

    /// <summary>
    /// Creates a notification type from the dto.
    /// </summary>
    /// <exception cref="ArgumentException">If the application in the dto is not <see cref="PlatformModule"/>.</exception>
    internal static NotificationType FromDto(NotificationTypeDto dto)
    {
        return dto.Application != PlatformModule ? throw new ArgumentException($"Cannot parse notifications for different modules: expected {PlatformModule} but got {dto.Application}") : new NotificationType(dto.Title);
    }

    /// <summary>
    /// Creates a notification from this notification type.
    /// </summary>
    public Notification ToNotification(string title, string description, Dictionary<string, string> templateValues, IList<NotificationLink> links, DateTime? occurrenceTimestamp = null)
    {
        return new Notification(this, title, description, templateValues, links, occurrenceTimestamp);
    }
}