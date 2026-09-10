using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvPosition = Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.AgvPosition;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.MqttMessageHandlers;

/// <summary>
/// Provides a consumer for instant actions.
/// </summary>
[UsedImplicitly]
public class MqttInstantActionsConsumer(ILogger<MqttInstantActionsConsumer> logger, IAgvOperatingModeCache operatingModeCache) :
    IConsumer<MqttInstantActions>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<MqttInstantActions> context)
    {
        if (!await operatingModeCache.ExistsAsync(context.Message.SerialNumber.ToGuid()))
        {
            return;
        }
        logger.LogInformation("Instant actions received for agv {Manufacturer}/{SerialNumber}", context.Message.Manufacturer, context.Message.SerialNumber);

        await ExtractInitPosition(context);
        await ExtractChargingActions(context);
        await ExtractOrderCancellation(context);
    }

    private async Task ExtractInitPosition(ConsumeContext<MqttInstantActions> context)
    {
        var initToPositionAction = context.Message.Actions.SingleOrDefault(a => a.ActionType == Vda5050Specifications.ActionTypes.InitPosition);
        if (initToPositionAction == null || initToPositionAction.ActionParameters == null)
        {
            return;
        }
        logger.LogInformation("Instant action initPosition received for agv {Manufacturer}/{SerialNumber}", context.Message.Manufacturer, context.Message.SerialNumber);
        string? lastNodeId = initToPositionAction.ActionParameters.GetStringParameter(Vda5050Specifications.Parameters.LastNodeId);
        float? x = initToPositionAction.ActionParameters.GetFloatParameter(Vda5050Specifications.Parameters.X);
        float? y = initToPositionAction.ActionParameters.GetFloatParameter(Vda5050Specifications.Parameters.Y);
        float? theta = initToPositionAction.ActionParameters.GetFloatParameter(Vda5050Specifications.Parameters.Theta);
        if (x == null || y == null || theta == null)
        {
            return;
        }
        await context.Publish(new UpdatePosition(context.Message.SerialNumber.ToGuid(), new AgvPosition { Theta = theta.Value, X = x.Value, Y = y.Value, Z = 0, PositionInitialized = true }, lastNodeId));
    }

    private async Task ExtractChargingActions(ConsumeContext<MqttInstantActions> context)
    {
        if (context.Message.Actions.Any(a => a.ActionType == Vda5050Specifications.ActionTypes.StartCharging))
        {
            logger.LogInformation("Instant action startCharging received for agv {Manufacturer}/{SerialNumber}", context.Message.Manufacturer, context.Message.SerialNumber);
            await context.Publish(new StartCharging(context.Message.SerialNumber.ToGuid()));
            return;
        }
        if (context.Message.Actions.Any(a => a.ActionType == Vda5050Specifications.ActionTypes.StopCharging))
        {
            logger.LogInformation("Instant action stopCharging received for agv {Manufacturer}/{SerialNumber}", context.Message.Manufacturer, context.Message.SerialNumber);
            await context.Publish(new StopCharging(context.Message.SerialNumber.ToGuid()));
        }
    }

    private async Task ExtractOrderCancellation(ConsumeContext<MqttInstantActions> context)
    {
        var cancelOrderAction = context.Message.Actions.SingleOrDefault(a => a.ActionType == Vda5050Specifications.ActionTypes.CancelOrder);
        if (cancelOrderAction != null)
        {
            logger.LogInformation("Instant action cancelOrder received for agv {Manufacturer}/{SerialNumber}", context.Message.Manufacturer, context.Message.SerialNumber);
            await context.Publish(new AbortOrder(context.Message.SerialNumber.ToGuid(), cancelOrderAction.ActionId));
        }
    }
}