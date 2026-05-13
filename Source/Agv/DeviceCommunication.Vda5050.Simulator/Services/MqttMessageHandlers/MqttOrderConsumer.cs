using AutoMapper;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.MqttMessageHandlers;

/// <summary>
/// Handles incoming messages on the order topic
/// </summary>
[UsedImplicitly]
public class MqttOrderConsumer(ILogger<MqttOrderConsumer> logger, IMapper mapper): 
    IConsumer<MqttOrder>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<MqttOrder> context)
    {
        string serialNumber = context.Message.SerialNumber;
        var order = mapper.Map<Order>(context.Message);

        logger.LogInformation("Order received for agv {SerialNumber}/{CorrelationId}: {OrderId}/{OrderUpdateId}", serialNumber, serialNumber.ToGuid(), context.Message.OrderId, context.Message.OrderUpdateId);

        if (context.Message.OrderUpdateId == 0)
        {
            logger.LogInformation("Starting order for agv {SerialNumber}/{CorrelationId}: {OrderId}/{OrderUpdateId}", serialNumber, serialNumber.ToGuid(), context.Message.OrderId, context.Message.OrderUpdateId);
            await context.Publish(new StartOrder(serialNumber.ToGuid(), order));
            return;
        }
        logger.LogInformation("Updating order for agv {SerialNumber}/{CorrelationId}: {OrderId}/{OrderUpdateId}", serialNumber, serialNumber.ToGuid(), context.Message.OrderId, context.Message.OrderUpdateId);
        await context.Publish(new UpdateOrder(serialNumber.ToGuid(), order));
    }
}