using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.HeadIds;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;

/// <summary>
/// Handles instant action related messages.
/// </summary>
[UsedImplicitly]
public class InstantActionsCommandHandler(ILogger<InstantActionsCommandHandler> logger, IAgvRepository agvRepository, ISequenceCounter<IInstantActionsHeaderId> instantActionHeaderId) :
IConsumer<StartInstantAction>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<StartInstantAction> context)
    {
        string actionType = context.Message.AgvInstantActionType;
        Dictionary<string, object>? parameters = context.Message.Parameters;
        var actionCorrelationId = context.Message.CorrelationId;
        string agvSerialNumber = context.Message.AgvSerialNumber;

        var agv = await agvRepository.GetRequiredAgvAsync(agvSerialNumber.ToGuid());
        string routingKey = agv.GetRoutingKey(Vda5050Specifications.Subtopics.InstantActions);
        logger.LogInformation("Received StartInstantAction: {ActionType}/{CorrelationId} for AGV:{AgvSerialNumber}, sending to {RoutingKey} ", actionType, actionCorrelationId, agvSerialNumber, routingKey);

        instantActionHeaderId.Increase();
        await context.PublishMqtt(new MqttInstantActions()
        {
            HeaderId = instantActionHeaderId.Current,
            Timestamp = DateTimeOffset.UtcNow,
            Version = Vda5050Specifications.Version,
            Manufacturer = agv.Manufacturer,
            SerialNumber = agv.SerialNumber,
            Actions =
            [
                new MqttInstantAction
                {
                    ActionType = actionType,
                    ActionId = actionCorrelationId.ToString(),
                    ActionParameters = parameters?.Select(p => new MqttActionParameter {Key = p.Key, Value = p.Value}).ToList(),
                    BlockingType = context.Message.BlockingType ?? BlockingType.NONE
                }
            ]
        }, routingKey, context.CancellationToken);
    }
}