using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.TriggerDefinition;
using Gebhardt.Acc.TriggerService.Domain.Services;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Gebhardt.Acc.TriggerService.Configuration;

namespace Gebhardt.Acc.TriggerService.Services;

/// <summary>
///  Provides a service to configure triggers.
/// </summary>
public class TriggerConfigurator(ITriggerManager triggerManager, IBus bus, TriggerServiceConfiguration config) : BackgroundService
{
    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IReadOnlyCollection<ITrigger> triggers = await triggerManager.LoadTriggers();
            foreach (var trigger in triggers)
            {
                await bus.Publish(new TriggerConfigured(trigger.GetProperties(), trigger.GetConfigurationParameters().Values), stoppingToken);
            }
            await Task.Delay(config.TriggerReloadIntervalMs, stoppingToken);
        }
    }
}