using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Handles instant action related messages.
/// </summary>
[UsedImplicitly]
public class InstantActionsConsumer(ILogger<InstantActionsConsumer> logger) :
IConsumer<StartInstantAction>
{
    /// <summary>
    /// Handles the <see cref="StartInstantAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StartInstantAction> context)
    {
        string actionType = context.Message.AgvInstantActionType;
        Dictionary<string, object>? parameters = context.Message.Parameters;
        var correlationId = context.Message.CorrelationId;
        string agvSerialNumber = context.Message.AgvSerialNumber;
        logger.LogInformation("Received StartInstantAction: {ActionType}/{CorrelationId} for AGV:{AgvId} ", actionType, correlationId, agvSerialNumber);

        if (string.IsNullOrEmpty(agvSerialNumber))
        {
            throw new AssignedAgvNotFoundException(agvSerialNumber, correlationId);
        }

        await context.Publish(new MqttInstantActions
        {
            SerialNumber = agvSerialNumber,
            Actions = [new MqttInstantAction { ActionId = correlationId.ToString(), ActionType = actionType, Parameters = parameters }]
        });

        logger.LogInformation("Sent instant actions: {ActionType} for AGV:{AgvId} ", actionType, agvSerialNumber);
    }
}