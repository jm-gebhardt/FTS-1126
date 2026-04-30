using Gebhardt.Acc.Common.Notification.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.Acc.Common.Notification;

/// <summary>
/// A singleton manager for sending notifications.
/// </summary>
[PublicAPI]
public interface INotificationManager : IHostedService
{
    /// <summary>
    /// Sends a notification.
    /// </summary>
    /// <param name="notification">The notification to send.</param>
    Task SendNotification(Models.Notification notification);

    /// <summary>
    /// Attempts to register a notification type with the notification center.
    /// </summary>
    Task TryRegisterNotificationType(NotificationType notificationType);

    /// <summary>
    /// This notification manager is no longer usable and will fail silently.
    /// </summary>
    bool Burnt { get; }
}