using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.TriggerDefinition;
using Gebhardt.Acc.TriggerService.Domain.Services;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Gebhardt.Acc.TriggerService.Configuration;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TriggerService.Services;

/// <summary>
///  Provides a service to configure triggers.
/// </summary>
public class TriggerConfigurator(ITriggerManager triggerManager, IBus bus, TriggerServiceConfiguration config, ILogger<TriggerConfigurator> logger) : BackgroundService
{
    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (config.TriggerReloadIntervalMs < 0)
        { 
            logger.LogInformation("Loading triggers only once");
            await LoadTriggers(stoppingToken);
            return;
        }
        logger.LogInformation("Loading triggers every {ReloadInterval} ms", config.TriggerReloadIntervalMs);
        while (!stoppingToken.IsCancellationRequested)
        {
            await LoadTriggers(stoppingToken);
            await Task.Delay(config.TriggerReloadIntervalMs, stoppingToken);
        }
    }

    private async Task LoadTriggers(CancellationToken stoppingToken)
    {
        IReadOnlyCollection<IFfcTrigger> triggers = await triggerManager.LoadTriggers();
        foreach (var trigger in triggers)
        {
            await bus.Publish(new TriggerConfigured(trigger.GetProperties(), trigger.GetDefaultConfigurationParameters().Values), stoppingToken);
        }
    }
}