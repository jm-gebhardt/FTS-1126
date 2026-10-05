using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Notification.Models;
using Gebhardt.Core.Abstractions.Application;
using Gebhardt.Core.NotificationCenter.Contracts.Commands;
using Gebhardt.Core.NotificationCenter.Contracts.Queries;
using Gebhardt.Core.NotificationCenter.Contracts.QueryResultDtos;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1873 // https://github.com/dotnet/sdk/pull/51818

namespace Gebhardt.Acc.Common.Notification;

/// <inheritdoc cref="INotificationManager"/>
[PublicAPI]
public class NotificationManager(IClientFactory clientFactory, IBus bus, ILogger<NotificationManager> logger, NotificationManagerConfiguration configuration) : INotificationManager
{
    private readonly ConcurrentDictionary<NotificationType, bool> _managedNotifications = new();
    private readonly ConcurrentDictionary<NotificationType, ConcurrentQueue<Models.Notification>> _unregisteredNotificationQueues = new();

    /// <inheritdoc />
    public bool Burnt { get; private set; }

    private long Retries
    {
        get => Interlocked.Read(ref field);
        set => Interlocked.Exchange(ref field, value);
    } = 0;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var getNotificationTypesClient = clientFactory.CreateRequestClient<GetNotificationTypesQuery>();
        Response<GetNotificationTypesQueryResult>? getNotificationsResult = null;

        while (getNotificationsResult == null && Retries < configuration.MaximumConnectionRetries)
        {
            try
            {
                getNotificationsResult = await getNotificationTypesClient.GetResponse<GetNotificationTypesQueryResult>(new GetNotificationTypesQuery(), cancellationToken);
            }
            catch (MassTransitException ex)
            {
                logger.LogWarning("Error in initial connection to notification center, Retries: {retries}, {error}", Retries, ex.Message);
                Retries++;
                await Task.Delay(configuration.ConnectionRetryDelayMs, cancellationToken);
            }
        }
        if (getNotificationsResult == null)
        {
            logger.LogCritical("Cannot establish connection to the notification center, retried {Retries} times unsuccessfully. Check if the notification center was deployed correctly and has access to the same MassTransit bus. Subsequent attempts to use this notification manager will fail silently.", Retries);
            Burnt = true;
            return;
        }

        var registeredNotifications = getNotificationsResult.Message.NotificationTypes.Where(nt => nt.Application == NotificationType.PlatformModule).Select(NotificationType.FromDto).ToList();
        var unregisteredNotifications = configuration.NotificationTypes.Except(registeredNotifications).ToList();

        foreach (var registeredNotification in registeredNotifications)
        {
            _managedNotifications.TryAdd(registeredNotification, true);
        }
        var registrationClient = clientFactory.CreateRequestClient<RegisterNotificationTypeCommand>();

        var tasks = unregisteredNotifications.Select(n => registrationClient.GetResponse<Result>(new RegisterNotificationTypeCommand(n.Name, [n.Name], NotificationType.PlatformModule), cancellationToken));

        var results = await Task.WhenAll(tasks);
        IEnumerable<(NotificationType notificationType, bool isRegistered)> zippedResults = unregisteredNotifications.Zip(results.Select(r => r.Message.IsSuccess));

        foreach ((var notificationType, bool isRegistered) in zippedResults)
        {
            _managedNotifications.TryAdd(notificationType, isRegistered);
        }
    }

    private bool IsRegistered(NotificationType notificationType) => _managedNotifications.TryGetValue(notificationType, out bool value) && value;

    /// <inheritdoc/>
    public async Task SendNotification(Models.Notification notification)
    {
        if (Burnt)
        {
            return;
        }

        if (IsRegistered(notification.Type))
        {
            await bus.Publish(new RaiseNotificationCommand(notification.ToDto()));
            return;
        }

        if (!_unregisteredNotificationQueues.TryGetValue(notification.Type, out var notifications))
        {
            notifications ??= new ConcurrentQueue<Models.Notification>();
            _unregisteredNotificationQueues.TryAdd(notification.Type, notifications);
        }

        if (notifications.Count >= configuration.MaximumQueuedNotifications)
        {
            logger.LogInformation("Queue limit exceeded for type {notificationType}", notification.Type);
            if (configuration.MaximumExceededPolicy is QueueMaximumExceededPolicy.DiscardNewest || !notifications.TryDequeue(out _))
            {
                logger.LogWarning("Could not add notification {notification} to queue; limit exceeded and could not dequeue", notification.Title);
                return;
            }
        }
        notifications.Enqueue(notification);
    }

    /// <inheritdoc/>
    public async Task TryRegisterNotificationType(NotificationType notificationType)
    {
        if (Burnt)
        {
            return;
        }

        var client = clientFactory.CreateRequestClient<RegisterNotificationTypeCommand>();

        if (IsRegistered(notificationType))
        {
            return;
        }

        var response = await client.GetResponse<Result>(new RegisterNotificationTypeCommand(notificationType.Name, [notificationType.Name], NotificationType.PlatformModule));

        bool isRegistered = response.Message.IsSuccess;

        if (!_managedNotifications.ContainsKey(notificationType))
        {
            _managedNotifications.TryAdd(notificationType, isRegistered);
        }
        else
        {
            _managedNotifications.TryUpdate(notificationType, isRegistered, !isRegistered);
        }

        if (isRegistered)
        {
            await SendQueuedNotifications(notificationType);
        }
    }

    private async Task SendQueuedNotifications(NotificationType notificationType)
    {
        if (_unregisteredNotificationQueues.TryRemove(notificationType, out var notifications))
        {
            var tasks = notifications.Select(n => bus.Publish(new RaiseNotificationCommand(n.ToDto())));

            await Task.WhenAll(tasks);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}