using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.StationCommunication.Domain.Models;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.StationCommunication.SimulationMode;

/// <summary>
/// Publishes <see cref="StationIsOnlineChanged"/> for every configured station.
/// </summary>
[UsedImplicitly]
public class SimulationStationOnlinePublisher(
    IPlcConnectionRepository connectionRepository,
    StationConfiguration stationConfiguration,
    IBus bus,
    ILogger<SimulationStationOnlinePublisher> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Simulation: waiting for station configuration to be loaded");
        await connectionRepository.WaitForInitializationAsync(stoppingToken);

        logger.LogInformation("Simulation: publishing online state for all {Count} stations", stationConfiguration.Stations.Count);

        foreach (var station in stationConfiguration.Stations)
        {
            var stationId = station.Name.ToGuid();
            await bus.Publish(new StationIsOnlineChanged(stationId, true), stoppingToken);
            logger.LogInformation("Simulation: station {StationName} ({StationId}) published as online", station.Name, stationId);
        }
    }
}