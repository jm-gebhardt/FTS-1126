using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.HeadIds;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Graph;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;

/// <summary>
/// Handles requests for resending agv related information, e.g. <see cref="RequestAgvData"/> and <see cref="RequestAgvGraph"/>.
/// </summary>
[UsedImplicitly]
public class ResendCommandHandler(
    ILogger<ResendCommandHandler> logger,
    IAgvRepository agvRepository,
    IFactsheetRequestCacheProvider factsheetRequestCache,
    ISequenceCounter<IInstantActionsHeaderId> instantActionSequenceCounter):
     IConsumer<RequestAgvData>,
     IConsumer<RequestAgvGraph>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestAgvData> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        string agvSerialNumber = context.Message.SerialNumber;
        logger.LogInformation("Requesting factsheet for agv {AgvSerialNumber}/{AgvCorrelationId}", agvSerialNumber, agvCorrelationId);
        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);
        if(agv == null)
        {
            logger.LogError("Can only request factsheet for agv {AgvSerialNumber}/{AgvCorrelationId} once the agv has connected", agvSerialNumber, agvCorrelationId);
            return;
        }
        instantActionSequenceCounter.Increase();
        await context.PublishMqtt(new MqttInstantActions()
        {
            HeaderId = instantActionSequenceCounter.Current,
            Timestamp = DateTimeOffset.UtcNow,
            Version = Vda5050Specifications.Version,
            Manufacturer = agv.Manufacturer,
            SerialNumber = agv.SerialNumber,
            Actions =
           [
               new MqttInstantAction
                {
                    ActionType = Vda5050Specifications.ActionTypes.RequestFactsheet,
                    ActionId = Guid.NewGuid().ToString(),
                    BlockingType = BlockingType.NONE
                }
           ]
        }, agv.GetRoutingKey(Vda5050Specifications.Subtopics.InstantActions), context.CancellationToken);
        factsheetRequestCache.ConcurrentDict.AddOrUpdate(agvCorrelationId, true, (_, _) => true);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestAgvGraph> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        string serialNumber = context.Message.SerialNumber;

        logger.LogDebug("RequestAgvGraph received for AGV {AgvSerialNumber}/{AgvCorrelationId}", serialNumber, agvCorrelationId);

        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);

        if (agv == null)
        {
            logger.LogDebug("No Agv found for AGV {AgvSerialNumber}/{AgvCorrelationId}, skipping", serialNumber, agvCorrelationId);
            return;
        }

        logger.LogDebug("Publishing graph regeneration for AGV {AgvSerialNumber}/{AgvCorrelationId}", serialNumber, agvCorrelationId);
        await context.Publish(new PublishAgvGraph(agvCorrelationId, serialNumber, agv.VehicleTypeId));
    }
}