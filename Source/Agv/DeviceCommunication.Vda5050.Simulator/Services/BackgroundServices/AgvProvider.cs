using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.BackgroundServices;

/// <summary>
/// Provides a service to generate simulated agvs.
/// </summary>
public class AgvProvider(Options options, ILogger<AgvProvider> logger, IBus bus):BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IEnumerable<Task> tasks = options.SimulatedSerialNumbers().Select(serialNumber => bus.Publish(new CreateAgv(new AgvConfiguration(serialNumber, options.MapId, options.Manufacturer)), stoppingToken));
        logger.LogInformation("Creating {Agvs} with base serial number {Base}", options.Agvs, options.SerialNumberBase);
        await Task.WhenAll(tasks);
    }
}