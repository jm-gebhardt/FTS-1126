using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Extensions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Execute processing of a transport order messages.
/// </summary>
[UsedImplicitly]
public class StartActionCommandConsumer : IConsumer<StartActionCommand>
{
    private readonly ILogger<StartActionCommandConsumer> _logger;
    private readonly IAgvRepository _agvRepository;
    private readonly IGraphRepository _graphRepository;

    /// <summary>
    /// Create dummy consumer with message handler delegate
    /// </summary>
    public StartActionCommandConsumer(ILogger<StartActionCommandConsumer> logger, IAgvRepository agvRepository, IGraphRepository graphRepository)
    {
        _logger = logger;
        _agvRepository = agvRepository;
        _graphRepository = graphRepository;
    }

    /// <summary>
    /// Handle message <see cref="StartActionCommand"/>. and send an order to the AGV of Type Transport, RecordType Order
    /// </summary>
    /// <param name="context">Context of the consumer wrapping details surrounding the inbound message.</param>
    /// <exception cref="AssignedAgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<StartActionCommand> context)
    {
        var action = context.Message.Action;
        var actionType = action.ActionType;
        if (action.IsMoveToCoordinate)
        {
            await MoveToCoordinate(context);
            return;
        }

        string targetNodeId = action.Target.NodeId!;

        List<object> routeNodes = [];
        routeNodes.AddRange(action.Nodes.Select(graphNodeId => _graphRepository.GetMqttNode(graphNodeId)).OfType<MqttNode>());
        List<object> routeEdges = [];
        routeEdges.AddRange(action.Edges.Select(graphEdgeGuid => _graphRepository.GetMqttEdge(graphEdgeGuid)).OfType<MqttEdge>());

        OrderType orderType;
        StationInfo? sourceToSend = null;
        StationInfo? destinationToSend = null;
        string agvSerialNumber = context.Message.SerialNumber;

        bool agvShouldRotate = action.InfoToAgv?.NeedsRotation() ?? false;
        double? liftHeight = action.InfoToAgv?.GetLiftHeight() ?? null;

        var assignedAgv = context.Message.CorrelationId;

        if (string.IsNullOrEmpty(agvSerialNumber))
        {
            throw new AssignedAgvNotFoundException(assignedAgv, agvSerialNumber, action);
        }

        switch (actionType)
        {
            case ActionType.Pick:
                orderType = OrderType.Pick;
                sourceToSend = new StationInfo
                {
                    Id = targetNodeId,
                    Rotate = agvShouldRotate,
                    LiftHeight = liftHeight
                };
                break;

            case ActionType.Drop:
                orderType = OrderType.Drop;
                destinationToSend = new StationInfo
                {
                    Id = targetNodeId,
                    LiftHeight = liftHeight
                };
                break;

            case ActionType.Move:
                orderType = OrderType.Move;
                destinationToSend = new StationInfo
                {
                    Id = targetNodeId
                };
                break;

            case ActionType.Charge:
                orderType = OrderType.Charge;
                destinationToSend = new StationInfo
                {
                    Id = targetNodeId
                };
                break;

            default:
                throw new InvalidOperationException($"Can't handle {actionType} yet");
        }

        // We are using the ActionCorrelationId as "orderId"
        // And we are transferring the Action's Guid
        await context.PublishMqtt(new MqttOrder(action.CorrelationId.ToString())
        {
            SerialNumber = agvSerialNumber,
            OrderType = orderType,
            OrderId = action.CorrelationId.ToString(),
            RecordType = OrderRecordType.Order,
            Stations = new Stations
            {
                FromStation = sourceToSend,
                ToStation = destinationToSend
            },
            Route = new Route()
            {
                Edges = routeEdges,
                Nodes = routeNodes
            }
        }, context.CancellationToken);

        FeedAgvRepository(assignedAgv, action);
        _logger.LogInformation("Sent transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
            action.CorrelationId, assignedAgv, agvSerialNumber, targetNodeId);
    }

    private async Task MoveToCoordinate(ConsumeContext<StartActionCommand> context)
    {
        var action = context.Message.Action;
        var actionType = action.ActionType;
        if (actionType != ActionType.Move)
        {
            throw new InvalidOperationException($"Can't handle {actionType} for move to coordinate");
        }
        string agvSerialNumber = context.Message.SerialNumber;
        var assignedAgv = context.Message.CorrelationId;
      
        await context.PublishMqtt(new MqttOrder(action.CorrelationId.ToString())
        {
            SerialNumber = agvSerialNumber,
            OrderType = OrderType.Move,
            OrderId = action.CorrelationId.ToString(),
            RecordType = OrderRecordType.Order,
            Coordinates = new Coordinates
            {
                ToTheta = action.Target.Position!.Theta,
                ToX = action.Target.Position!.X,
                ToY = action.Target.Position!.Y
            }
        }, context.CancellationToken);

        FeedAgvRepository(assignedAgv, action);
        _logger.LogInformation("Sent transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} to coordinates: {Coordinates}",
            action.CorrelationId, assignedAgv, agvSerialNumber, action.Target.Position!);
    }

    private void FeedAgvRepository(Guid correlationId, AgvAction agvAction)
    {
        _agvRepository.AddOrUpdateAgvAction(correlationId, agvAction);
    }
}