using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.Common.Notification;

/// <summary>
/// Contains extension methods for the service collection.
/// </summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Contains extension methods for the service collection.
    /// </summary>
    extension(IServiceCollection serviceCollection)
    {
        /// <summary>
        /// Adds the notification manager to the service collection.
        /// </summary>
        [PublicAPI]
        public IServiceCollection AddNotifications(NotificationManagerConfiguration configuration)
        {
            serviceCollection.AddSingleton(_ => configuration);
            serviceCollection.AddSingleton<NotificationManager>();
            serviceCollection.AddSingleton<INotificationManager>(s => s.GetRequiredService<NotificationManager>());
            serviceCollection.AddHostedService(s => s.GetRequiredService<NotificationManager>());
            return serviceCollection;
        }
    }
}