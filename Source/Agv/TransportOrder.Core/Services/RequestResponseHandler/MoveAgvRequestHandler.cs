using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers to (manually) move an agv.
/// </summary>
/// <inheritdoc cref="MoveAgvRequestHandler"/>>
[UsedImplicitly]
public class MoveAgvRequestHandler(
    IAgvDb agvDb,
    IBus bus,
    INodeStateRepository nodeStateRepository,
    IParkPointService parkPointService,
    IChargingPointManager chargingPointManager
) :
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
        switch (targetNode.Type)
        {
            case AgvGraph.NodeType.Charge:
                await chargingPointManager.ReserveChargerAsync(targetNode, agvCorrelationId);
                break;

            case AgvGraph.NodeType.Park:
                await parkPointService.ReserveParkAsync(targetNode.Id, agvCorrelationId);
                break;

            default:
                throw new ArgumentOutOfRangeException($"Unable to reserve node type: {targetNode.Type}");
        }

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