using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.StationCommunication.Services.CommandHandler;

/// <summary>
/// Re-publishes <see cref="StationIsOnlineChanged"/> for all monitored stations.
/// </summary>
[UsedImplicitly]
public class StationOnlineStateCommandHandler(
    IPlcConnectionRepository connectionRepository,
    ILogger<StationOnlineStateCommandHandler> logger) :
    IConsumer<StationOnlineStatesRequested>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<StationOnlineStatesRequested> context)
    {
        var states = connectionRepository.GetAllStationStates().ToList();
        logger.LogInformation("Republishing online state for {Count} stations", states.Count);

        foreach (var (stationId, isOnline) in states)
        {
            await context.Publish(new StationIsOnlineChanged(stationId, isOnline));
        }
    }
}