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
    IConsumer<ClearAgvCurrentOrder>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<ClearAgvCurrentOrder> context)
    {
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);

        if (agv == null)
        {
            logger.LogDebug("No Agv found for claring agv current order for AGV {AgvCorrelationId}, skipping", agvCorrelationId);
            return;
        }
        agv.CurrentOrder = null;
        agv.CurrentFleetManagerActionId = null;
        await agvRepository.AddOrUpdateAsync(agv);
        logger.LogDebug("Cleared current order reference for AGV {AgvCorrelationId}", agvCorrelationId);
    }
}