using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.Common.Messages.Stations.Requests;
using Gebhardt.Acc.PrismatCommunication.Domain.Exceptions;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.SystemStatus;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Services.BackgroundServices;

/// <summary>
/// Provides initialization for the service.
/// </summary>
[UsedImplicitly]
public class InitializationProvider(
    KaeserGraphConfiguration configuration,
    ILogger<InitializationProvider> logger,
    IBus bus,
    IServiceProvider serviceProvider,
    IClientFactory clientFactory
) : BackgroundService
{
    private readonly TimeSpan _configLoadingWaitTime = TimeSpan.FromSeconds(5);

    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(InitializeStationGroups(), InitializeShortLoopTargetStation(), InitializeTriggerMapping());
    }

    private async Task InitializeStationGroups()
    {
        while (!configuration.IsLoaded)
        {
            await Task.Delay(_configLoadingWaitTime);
        }
        logger.LogInformation("Configuration is loaded, got {Count} stationGroups", configuration.StationGroups.Count);

        IRequestClient<AddOrUpdateStationGroup> stationGroupRequestClient = clientFactory.CreateRequestClient<AddOrUpdateStationGroup>();
        
        foreach ((string stationGroup, ICollection<Station> stations) in configuration.StationGroups)
        {
            await stationGroupRequestClient.GetResponse<Success>(new AddOrUpdateStationGroup(stations, stationGroup.ToGuid()));
        }
        if (configuration.UseAklAgvLimit.HasValue)
        {
            logger.LogInformation("Initializing UseAklAgvLimit {Flag}", configuration.UseAklAgvLimit);
            await bus.Publish(new UseAklAgvLimit(configuration.UseAklAgvLimit.Value));
        }
    }

    private async Task InitializeShortLoopTargetStation()
    {
        using var scope = serviceProvider.CreateScope();

        var targetStationRepository = scope.ServiceProvider.GetRequiredService<IShortLoopWeTargetStationRepository>();

        IRequestClient<GetLockedStations> lockedStationsRequestClient = clientFactory.CreateRequestClient<GetLockedStations>();

        logger.LogInformation("Initializing short loop WE target station");
        try
        {
            var shortLoopTarget = await targetStationRepository.GetTargetStation();
            try 
            {
                Response<GotLockedStations> lockedStations = await lockedStationsRequestClient.GetResponse<GotLockedStations>(new GetLockedStations());
                IEnumerable<Task> tasks = shortLoopTarget.LockedStations.Except(lockedStations.Message.Stations).Select(targetStationRepository.UnlockStation);
                tasks = tasks.Concat(lockedStations.Message.Stations.Select(targetStationRepository.LockStation));
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                logger.LogWarning("Could not retrieve locked stations, defaulting to empty: {reason}", e.Message);
            }
            configuration.SetShortLoopTarget(shortLoopTarget.Current);
        }
        catch (ShortLoopWeTargetStationNotFoundException ex)
        {
            logger.LogWarning("{Reason}, creating from default", ex.Message);
            string defaultTarget = configuration.GetShortLoopTarget();
            var shortLoopTarget = new ShortLoopWeTargetStation(defaultTarget, defaultTarget);

            try
            {
                Response<GotLockedStations> lockedStations = await lockedStationsRequestClient.GetResponse<GotLockedStations>(new GetLockedStations());
                shortLoopTarget.LockedStations.AddRange(lockedStations.Message.Stations);
            }
            catch (Exception e)
            {
                logger.LogWarning("Could not retrieve locked stations, defaulting to empty: {reason}", e.Message);
            }

            targetStationRepository.Add(shortLoopTarget);
            await targetStationRepository.SaveAsync();
        }
    }

    private async Task InitializeTriggerMapping()
    {
        logger.LogInformation("Initializing trigger mapping");
        try
        {
            IRequestClient<GetTriggerMapping> triggerRequestClient = clientFactory.CreateRequestClient<GetTriggerMapping>();

            Response<GotTriggerMapping> response = await triggerRequestClient.GetResponse<GotTriggerMapping>(new GetTriggerMapping());
            foreach (var localization in response.Message.TriggerLocalizations)
            {
                if (localization.NodeId != null)
                {
                    logger.LogInformation("Initializing mapping of trigger {Name} to {NodeId}", localization.TriggerName, localization.NodeId);
                    configuration.UpdateTriggerPointNodeId(localization.TriggerName, localization.NodeId.Value);
                    continue;
                }

                if (localization.AreaId != null)
                {
                    string nodeName = "AREA-" + localization.AreaId;
                    logger.LogInformation("Initializing mapping of trigger {Name} to {NodeId}", localization.TriggerName, nodeName);
                    configuration.UpdateTriggerPointNodeId(localization.TriggerName, nodeName.ToGuid());
                }
            }
        }
        catch (RequestTimeoutException)
        {
            logger.LogWarning("Got request time out, assuming there are no triggers or they have not been localized yet");
        }
    }
}