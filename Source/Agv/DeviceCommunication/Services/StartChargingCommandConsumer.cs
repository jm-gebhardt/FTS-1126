using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using JetBrains.Annotations;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using static Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvGraph;
using Gebhardt.StoreWare.Agv.Common.Configurations.Exceptions;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

/// <summary>
/// Starts charging for Agv on charging node
/// </summary>
[UsedImplicitly]
public class StartChargingCommandConsumer : IConsumer<StartChargingCommand>
{
    private readonly IAgvRegistry _agvRegistry;

    /// <summary>
    /// Create a new instance of <see cref="StartChargingCommandConsumer"/>
    /// </summary>
    /// <param name="agvRegistry"></param>
    public StartChargingCommandConsumer(IAgvRegistry agvRegistry)
    {
        _agvRegistry = agvRegistry;
    }

    /// <summary>
    /// Validates (checks current node, if charging node and not busy and if battery charge less than 100), then sends charging action to Agv
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<StartChargingCommand> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var agvInformation = _agvRegistry.GetAgvInformation(agvCorrelationId);

        if (agvInformation == null)
        {
            throw new NoContentException(nameof(agvInformation));
        }

        var agvStatusReceived = agvInformation.AgvStatusReceived;
        if (agvStatusReceived == null)
        {
            throw new NoContentException(nameof(agvStatusReceived));
        }

        var agvStatus = agvStatusReceived.AgvStatus;
        string? lastNodeId = agvStatus.LastNodeId;

        if (lastNodeId == null)
        {
            throw new NoContentException(nameof(lastNodeId));
        }

        if (agvStatus.Busy)
        {
            throw new NotInCorrectStateException("Agv charging cannot be started because agv is busy.");
        }

        if (agvStatus.BatteryCharge >= 100)
        {
            throw new NotInCorrectStateException("Agv charging cannot be started because agv is full charged");
        }

        NodeType? lastNodeType = agvInformation.AgvGraph?.Nodes.FirstOrDefault(node => node.Id == lastNodeId)?.Type;
        if (lastNodeType != NodeType.Charge)
        {
            throw new NotInCorrectStateException($"Agv charging cannot be started because agv is not in charging node. [{nameof(lastNodeId)} ({lastNodeId})] [{nameof(NodeType)} ({lastNodeType})]");
        }

        string agvId = agvStatusReceived.AgvId;
        var chargeAction = new AgvAction(
            Guid.NewGuid(),
            agvId,
            ActionType.Charge,
            string.Empty,
            Guid.Empty,
            null,
            lastNodeId);

        await context.Publish(new StartActionCommand(agvCorrelationId, chargeAction));
        await context.Publish(new ActionStartedEvent(chargeAction.CorrelationId, Guid.Empty));
        await context.RespondAsync(new BaseResult());
    }
}