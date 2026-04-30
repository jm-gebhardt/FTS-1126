using Gebhardt.Acc.Common.Notification.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Notification;

/// <summary>
/// Describes a policy for if a queue maximum is exceeded.
/// </summary>
[PublicAPI]
public enum QueueMaximumExceededPolicy
{
    /// <summary>
    /// Discards the oldest element.
    /// </summary>
    DiscardOldest,

    /// <summary>
    /// Discard the incoming element.
    /// </summary>
    DiscardNewest
}

/// <summary>
/// Configuration for <see cref="INotificationManager"/>
/// </summary>
/// <param name="NotificationTypes">The notification types that should be managed by this configuration manager.</param>
/// <param name="MaximumQueuedNotifications">The maximum number of notifications that should be collected when waiting for a notification type to register.</param>
/// <param name="MaximumExceededPolicy">The policy to use when a notification for a yet unregistered type is attempted to be queued, but the queue has reached maximum size.</param>
/// <param name="MaximumConnectionRetries">The maximum amount of retries when a connection to the notification center fails.</param>
/// <param name="ConnectionRetryDelayMs">The delay between connection retries.</param>
[PublicAPI]
public record NotificationManagerConfiguration(
    IReadOnlyCollection<NotificationType> NotificationTypes,
    int MaximumQueuedNotifications = 100,
    QueueMaximumExceededPolicy MaximumExceededPolicy = QueueMaximumExceededPolicy.DiscardOldest,
    long MaximumConnectionRetries = long.MaxValue,
    int ConnectionRetryDelayMs = 5000
);