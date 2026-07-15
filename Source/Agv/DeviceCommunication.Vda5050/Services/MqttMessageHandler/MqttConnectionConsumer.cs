using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;

/// <summary>
/// Handles the <see cref="MqttConnection"/> message.
/// </summary>
[UsedImplicitly]
public class MqttConnectionConsumer(ILogger<MqttConnectionConsumer> logger, IAgvRepository agvRepository) :
    IConsumer<MqttConnection>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<MqttConnection> context)
    {
        (_, _, _, string manufacturer, string serialNumber, var connectionState) = context.Message;

        var agvCorrelationId = serialNumber.ToGuid();

        if (connectionState is MqttConnectionState.Online)
        {
            logger.LogInformation("Agv {AgvSerialNumber}/{AgvCorrelationId} went online, waiting for state ", serialNumber, agvCorrelationId);

            var agv = await GetAgv(agvCorrelationId, manufacturer, serialNumber);
            string? routingKey = context.RoutingKey();
            if (routingKey != null)
            {
                agv.SetCommunicationSpecification(routingKey);
                await agvRepository.AddOrUpdateAsync(agv, context.CancellationToken);
            }
            return;
        }

        if (connectionState is MqttConnectionState.Offline or MqttConnectionState.ConnectionBroken)
        {
            logger.LogInformation("Last will received {AgvSerialNumber}/{AgvCorrelationId} with connection state {ConnectionState}", serialNumber, agvCorrelationId, connectionState);
            await context.Publish(new AgvLastWillReceived(agvCorrelationId));
        }
    }

    private async Task<Agv> GetAgv(Guid agvCorrelationId, string manufacturer, string serialNumber)
    {
        try
        {
            return await agvRepository.GetRequiredAgvAsync(agvCorrelationId);
        }
        catch (AgvNotFoundException)
        {
            return new Agv(agvCorrelationId, manufacturer, "", serialNumber);
        }
    }
}