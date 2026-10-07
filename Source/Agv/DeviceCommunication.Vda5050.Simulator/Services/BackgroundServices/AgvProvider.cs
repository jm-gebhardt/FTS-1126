using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.BackgroundServices;

/// <summary>
/// Provides a service to generate simulated agvs.
/// </summary>
public class AgvProvider(
    ILogger<AgvProvider> logger,
    ISimulatedAgvTypeConfigurationRepository configurationRepository,
    ISimulatedAgvSpawner spawner,
    IBus bus) : BackgroundService
{
    private async Task SpawnAsync(SimulatedAgvTypeConfiguration configuration, CancellationToken stoppingToken)
    {
        try
        {
            await bus.Publish(new FetchFactsheet(configuration.Id, configuration.AgvTypeIdentifier), stoppingToken);
            await spawner.ApplyAsync(null, configuration, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to create the simulated AGVs of agv type {AgvTypeIdentifier}", configuration.AgvTypeIdentifier);
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var configurations = await configurationRepository.GetAllAsync();

            logger.LogInformation("Creating simulated AGVs for {AgvTypes} configured AGV types", configurations.Count);
            foreach (var configuration in configurations)
            {
                await SpawnAsync(configuration, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Creating simulated AGVs was cancelled during shutdown");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to read the simulation configurations, no simulated AGVs were created");
        }
    }
}