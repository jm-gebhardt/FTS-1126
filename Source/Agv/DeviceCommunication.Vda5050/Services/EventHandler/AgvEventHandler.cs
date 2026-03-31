using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Graph;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;

/// <summary>
/// Handles <see cref="AgvAddedOrUpdated"/>.
/// </summary>
[UsedImplicitly]
public class AgvEventHandler(
    IAgvRepository agvRepository,
    ILogger<AgvEventHandler> logger) : IConsumer<AgvAddedOrUpdated>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AgvAddedOrUpdated> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        string serialNumber = context.Message.SerialNumber;

        var agv = await agvRepository.GetRequiredAgvAsync(agvCorrelationId);

        logger.LogDebug("AGV registered, trying to publish graph for {AgvSerialNumber}/{AgvCorrelationId} (vehicleTypeId: {VehicleTypeId})",
            serialNumber, agvCorrelationId, agv.VehicleTypeId);

        await context.Publish(new PublishAgvGraph(agv.CorrelationId, agv.SerialNumber, agv.VehicleTypeId));
    }
}