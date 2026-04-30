using Gebhardt.Acc.Common.Notification.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages;

/// <summary>
/// A collection of notification types raised by the transport order service.
/// </summary>
public static class TransportOrderNotifications
{
    /// <summary>
    /// Agv related notifications.
    /// </summary>
    public static readonly NotificationType AgvNotification = new("Agv");

    /// <summary>
    /// Station related notifications.
    /// </summary>
    [UsedImplicitly]
    public static readonly NotificationType StationNotification = new("Station");

    /// <inheritdoc cref="TransportOrderNotifications"/>
    public static readonly IReadOnlyList<NotificationType> NotificationTypes =
    [
        AgvNotification, StationNotification
    ];
}