using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Filters;

/// <summary>
/// Provides consume filters for mqtt messages sent by the agv.
/// </summary>
[UsedImplicitly]
public class MqttMessageConsumeFilter(
    ILogger<MqttMessageConsumeFilter> logger,
    UnTimedInMemoryFilter<MqttState> mqttStateFilter) :
    IFilter<ConsumeContext<MqttState>>
{
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