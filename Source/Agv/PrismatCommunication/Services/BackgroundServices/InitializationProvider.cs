using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.PrismatCommunication.Domain.Exceptions;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Services.BackgroundServices;

/// <summary>
/// Provides initialization for the service.
/// </summary>
[UsedImplicitly]
public class InitializationProvider(
    KaeserGraphConfiguration configuration,
    IShortLoopWeTargetStationRepository targetStationRepository,
    ILogger<InitializationProvider> logger,
    IRequestClient<AddOrUpdateStationGroup> stationGroupRequestClient,
    IRequestClient<GetQueueFillQuantities> queueRequestClient,
    IRequestClient<GetTriggerMapping> triggerRequestClient
) : BackgroundService
{
    private readonly TimeSpan _configLoadingWaitTime = TimeSpan.FromSeconds(5);

    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(InitializeStationGroups(), InitializeShortLoopTargetStation(), InitializeQueue(), InitializeTriggerMapping());
    }

    private async Task InitializeStationGroups()
    {
        while (!configuration.IsLoaded)
        {
            await Task.Delay(_configLoadingWaitTime);
        }
        logger.LogInformation("Configuration is loaded, got {Count} stationGroups", configuration.StationGroups.Count);
        foreach (var (stationGroup, stations) in configuration.StationGroups)
        {
            await stationGroupRequestClient.GetResponse<Success>(new AddOrUpdateStationGroup(stations, stationGroup.ToGuid()));
        }
    }

    private async Task InitializeShortLoopTargetStation()
    {
        logger.LogInformation("Initializing short loop WE target station");
        try
        {
            var shortLoopTarget = await targetStationRepository.GetTargetStation();
            configuration.SetShortLoopTarget(shortLoopTarget.Current);
        }
        catch (ShortLoopWeTargetStationNotFoundException ex)
        {
            logger.LogWarning("{Reason}, creating from default", ex.Message);
            string defaultTarget = configuration.GetShortLoopTarget();
            var shortLoopTarget = new ShortLoopWeTargetStation(defaultTarget, defaultTarget);
            targetStationRepository.Add(shortLoopTarget);
            await targetStationRepository.SaveAsync();
        }
    }

    private async Task InitializeQueue()
    {
        logger.LogInformation("Initializing queue occupation");
        configuration.SetQueueIsOccupied(false);
        try
        {
            Response<GotQueueFillQuantities> response = await queueRequestClient.GetResponse<GotQueueFillQuantities>(new GetQueueFillQuantities());
            if (response.Message.FillQuantities.Count > 0)
            {
                var queue = response.Message.FillQuantities.First();
                if (queue.Quantity > 0)
                {
                    configuration.SetQueueIsOccupied(true);
                    logger.LogInformation("Queue is occupied");
                }
            }
        }
        catch (RequestTimeoutException)
        {
            logger.LogWarning("Got request time out, assuming queue is empty or there is no queue");
        }
    }

    private async Task InitializeTriggerMapping()
    {
        logger.LogInformation("Initializing trigger mapping");
        try
        {
            Response<GotTriggerMapping> response = await triggerRequestClient.GetResponse<GotTriggerMapping>(new GetTriggerMapping());
            foreach (var localization in response.Message.TriggerLocalizations)
            {
                if (localization.NodeId != null)
                {
                    logger.LogInformation("Initializing mapping of trigger {Name} to {NodeId}", localization.TriggerName, localization.NodeId);
                    configuration.UpdateTriggerPointNodeId(localization.TriggerName, localization.NodeId);
                    continue;
                }

                if (localization.AreaId != null)
                {
                    string nodeId = "AREA-" + localization.AreaId;
                    logger.LogInformation("Initializing mapping of trigger {Name} to {NodeId}", localization.TriggerName, nodeId);
                    configuration.UpdateTriggerPointNodeId(localization.TriggerName, nodeId);
                }
            }
        }
        catch (RequestTimeoutException)
        {
            logger.LogWarning("Got request time out, assuming there are no triggers or they have not been localized yet");
        }
    }
}