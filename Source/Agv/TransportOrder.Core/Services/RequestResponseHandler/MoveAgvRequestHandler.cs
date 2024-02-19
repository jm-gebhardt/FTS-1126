using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Map;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers to (manually) move an agv.
/// </summary>
/// <inheritdoc cref="MoveAgvRequestHandler"/>>
[UsedImplicitly]
public class MoveAgvRequestHandler(IAgvDb agvDb, IBus bus, ILogger<MoveAgvRequestHandler> logger, INodeStateRepository nodeStateRepository) :
    IConsumer<MoveAgv>,
    IConsumer<ChargeAgv>
{
    /// <summary>
    /// Handles the <see cref="MoveAgv"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<MoveAgv> context)
    {
        (var agvCorrelationId, string targetNodeId, var creator) = context.Message;

        await agvDb.Agvs.ThrowIfAgvNotExistsAsync(agvCorrelationId);

        var targetNode = await agvDb.Graphs.GetNodeAsync(targetNodeId);

        if (targetNode.NeedsReservation())
        {
            await ReserveNodeForAgv(targetNode, agvCorrelationId);
        }
        var actionGroupId = Guid.NewGuid();
        await QueueNewActionGroup(context,
            [
                new AgvAction(Guid.NewGuid(),
                    ActionType.Move,
                    targetNode.Id,
                    actionGroupId)
            ],
            $"Manual {ActionType.Move}",
            agvCorrelationId,
            creator,
            actionGroupId);
        await context.RespondAsync(new AgvMoveSuccessful(actionGroupId));
    }

    /// <summary>
    /// Handles the <see cref="ChargeAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ChargeAgv> context)
    {
        (var agvCorrelationId, string chargerId) = context.Message;

        await agvDb.Agvs.ThrowIfAgvNotExistsAsync(agvCorrelationId);

        var node = await agvDb.Graphs.GetNodeAsync(chargerId);

        if (!node.IsCharger())
        {
            throw new InvalidNodeTypeException(node.Id, node.Type.ToString(), nameof(AgvGraph.NodeType.Charge));
        }

        await ReserveNodeForAgv(node, agvCorrelationId);

        var actionGroupId = Guid.NewGuid();
        await QueueNewActionGroup(context,
            [
                new AgvAction(Guid.NewGuid(), ActionType.Move, node.Id, actionGroupId)
                {
                    Sequence = 0
                },

                new AgvAction(Guid.NewGuid(), ActionType.Charge, node.Id, actionGroupId)
                {
                    Sequence = 1
                }
            ], $"Manual {nameof(ActionType.Charge)}",
            agvCorrelationId,
            ActionGroupCreator.TransportOrderWebApi,
            actionGroupId);
        await context.RespondAsync(new AgvMoveSuccessful(actionGroupId));
    }

    private async Task ReserveNodeForAgv(GraphNode targetNode, Guid agvCorrelationId)
    {
        var nodeState = await nodeStateRepository.GetNodeStateAsync(targetNode.Id);

        if (nodeState != null && nodeState.NodeState != NodeLockState.Free)
        {
            string state = nodeState.NodeState == NodeLockState.Occupied ? nameof(NodeLockState.Occupied) : nameof(NodeLockState.Reserved);
            logger.LogWarning("Could not reserve node for {AgvCorrelationId}, target node is {NodeState}", agvCorrelationId, state);
            throw new NoNodesAvailableException($"Target node is already {state}");
        }

        await nodeStateRepository.ReserveNodeAsync(targetNode.Id, agvCorrelationId);
        await PublishNodeStateChange(targetNode.Id);
    }

    private async Task QueueNewActionGroup(ConsumeContext context, ICollection<AgvAction> actions, string orderId, Guid agvCorrelationId, ActionGroupCreator creator, Guid actionGroupId)
    {
        var actionGroup = new ActionGroup(
            actionGroupId,
            orderId,
            actions,
            assignedAgv: agvCorrelationId,
            isManual: actions.All(a => a.ActionType != ActionType.Charge),
            creator: creator
        );
        IRequestClient<QueueActionGroup>? requestClient = context.CreateRequestClient<QueueActionGroup>(bus);
        await requestClient.GetResponse<Success>(new QueueActionGroup(actionGroup, true));
    }

    private async Task PublishNodeStateChange(string nodeId)
    {
        var nodeState = await nodeStateRepository.GetNodeStateAsync(nodeId);
        await bus.Publish(new NodeStateChanged(nodeState!));
    }
}