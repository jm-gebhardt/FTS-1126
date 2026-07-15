using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;

/// <summary>
/// Provides handlers for action-related commands.
/// </summary>
[UsedImplicitly]
public class AgvCommandHandler(ILogger<AgvCommandHandler> logger, IAgvRepository agvRepository) :
    IConsumer<ClearAgvCurrentOrder>,
    IConsumer<AddAgv>,
    IConsumer<UpdateAgvType>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<ClearAgvCurrentOrder> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);

        if (agv == null)
        {
            logger.LogDebug("No Agv found for clearing agv current order for AGV {AgvCorrelationId}, skipping", agvCorrelationId);
            return;
        }
        agv.ClearOrder();
        await agvRepository.AddOrUpdateAsync(agv, context.CancellationToken);
        logger.LogDebug("Cleared current order reference for AGV {AgvCorrelationId}", agvCorrelationId);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AddAgv> context)
    {
        (var correlationId, var agv, Guid? agvTypeId, bool hasValidConfiguration) = context.Message;
        var existingAgv = await agvRepository.GetAgvAsync(correlationId);
        if (existingAgv?.PublishedToFleetManager ?? false)
        {
            logger.LogWarning("Agv {AgvCorrelationId}/{AgvSerialNumber} already reported to fleet manager", correlationId, agv.SerialNumber);
            return;
        }
        agv.PublishedToFleetManager = true;
        await agvRepository.AddOrUpdateAsync(agv, context.CancellationToken);

        logger.LogInformation("New agv  {SerialNumber}/{AgvCorrelationId} added with valid configuration {HasValidConfiguration}, reporting discovery to fleet manager", agv.CorrelationId, agv.SerialNumber, hasValidConfiguration);
        await context.Publish(new NewAgvDiscovered(correlationId, agv.SerialNumber, agvTypeId, hasValidConfiguration));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<UpdateAgvType> context)
    {
        (var correlationId, var newTypeId, var agv, bool hasValidConfiguration, bool needsConfirmation) = context.Message;
        await agvRepository.AddOrUpdateAsync(agv, context.CancellationToken);
        await context.Publish(new AgvVehicleTypeChanged(correlationId, agv.SerialNumber, newTypeId, agv.VehicleTypeId, hasValidConfiguration, needsConfirmation));
    }
}