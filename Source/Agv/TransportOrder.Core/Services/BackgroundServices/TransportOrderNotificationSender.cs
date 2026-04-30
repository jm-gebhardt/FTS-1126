using Gebhardt.Acc.Common.Notification;
using Gebhardt.Acc.Common.Notification.Models;
using Gebhardt.Acc.TransportOrder.Messages;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

/// <inheritdoc />
public class TransportOrderNotificationSender(INotificationManager notificationManager): ITransportOrderNotificationSender
{
    /// <inheritdoc />
    public async Task SendAgvNotification(string title, string description, Dictionary<string, string> templateValues, IList<NotificationLink> links)
    {
        await notificationManager.SendNotification(TransportOrderNotifications.AgvNotification.ToNotification(
            title,
            description,
            templateValues,
            links
        ));
    }
}