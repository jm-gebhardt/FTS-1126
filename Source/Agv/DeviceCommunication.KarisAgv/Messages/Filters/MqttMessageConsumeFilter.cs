using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Filters;

/// <summary>
/// Provides consume filters for mqtt messages sent by the agv.
/// </summary>
[UsedImplicitly]
public class MqttMessageConsumeFilter(
    ILogger<MqttMessageConsumeFilter> logger,
    UnTimedInMemoryFilter<MqttOrderEvent> mqttOrderEventFilter,
    UnTimedInMemoryFilter<MqttState> mqttStateFilter) :
    IFilter<ConsumeContext<MqttOrderEvent>>,
    IFilter<ConsumeContext<MqttState>>
{
    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<MqttOrderEvent> context, IPipe<ConsumeContext<MqttOrderEvent>> next)
    {
        if (await mqttOrderEventFilter.ShouldActAsync(context.Message))
        {
            await next.Send(context);
            return;
        }
        logger.LogInformation("Not consuming duplication of {Message}", nameof(MqttOrderEvent));
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<MqttState> context, IPipe<ConsumeContext<MqttState>> next)
    {
        if (await mqttStateFilter.ShouldActAsync(context.Message))
        {
            await next.Send(context);
            return;
        }
        logger.LogInformation("Not consuming duplication of {Message}", nameof(MqttState));
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public void Probe(ProbeContext context)
    {
        context.CreateScope(nameof(MqttMessageConsumeFilter));
    }
}