using Gebhardt.Acc.Common.Notification.Models;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

/// <summary>
/// The notification sender for transport order.
/// </summary>
public interface ITransportOrderNotificationSender
{
    /// <summary>
    /// Sends an agv notification
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="description">The description of the notification.</param>
    /// <param name="templateValues">The template values of the notification.</param>
    /// <param name="links">The links of the notification.</param>
    Task SendAgvNotification(string title, string description, Dictionary<string, string> templateValues, IList<NotificationLink> links);
}