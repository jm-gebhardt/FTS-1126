using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for node-related commands.
/// </summary>
[UsedImplicitly]
public class NodeCommandHandler(
    INodeStateRepository nodeStateRepository, 
    IStationRepository stationRepository,
    IActionGroupRepository actionGroupRepository, 
    ILogger<NodeCommandHandler> logger) :
    IConsumer<FreeNode>,
    IConsumer<UpdateNodeOccupancy>,
    IConsumer<ClearNodesFromAgv>
{
    /// <summary>
    /// Handles the <see cref="FreeNode"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FreeNode> context)
    {
        string nodeId = context.Message.NodeId;
        var station = await stationRepository.GetStation(nodeId);
        if (!await nodeStateRepository.FreeNodeAsync(nodeId))
        {
            logger.LogWarning("Could not free node {NodeId}, node is locked.", nodeId);
        }

        var nodeState = await nodeStateRepository.GetNodeStateAsync(nodeId);
        if (nodeState == null)
        {
            logger.LogWarning("Could not retrieve node state for node {NodeId}", nodeId);
            return;
        }
        await context.Publish(new NodeStateChanged(nodeState, station.Id));
    }

    /// <summary>
    /// Handles the <see cref="ClearNodesFromAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ClearNodesFromAgv> context)
    {
        ICollection<NodeLock> nodeLocksForAgv = await nodeStateRepository.GetNodeLocksForAgv(context.Message.AgvCorrelationId);
        ICollection<Task<bool>> freeNodeTasks = [];
        ICollection<Task> publishTasks = [];
        foreach (var node in nodeLocksForAgv)
        {
            freeNodeTasks.Add(nodeStateRepository.FreeNodeAsync(node.NodeId));
            publishTasks.Add(PublishNodeStateChange(context, node.NodeId));
        }
        await Task.WhenAll(freeNodeTasks);
        await Task.WhenAll(publishTasks);
    }

    /// <summary>
    /// Handles the <see cref="UpdateNodeOccupancy"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateNodeOccupancy> context)
    {
        (var agvCorrelationId, string? oldLastNodeId, string? newLastNodeId) = context.Message;
        logger.LogTrace("{MessageType} {AgvId}/{CorrelationId} received", nameof(UpdateNodeOccupancy), agvCorrelationId, agvCorrelationId);

        ICollection<NodeLock> nodeLocksForAgv = await nodeStateRepository.GetNodeLocksForAgv(agvCorrelationId);
        var oldLastNode = nodeLocksForAgv.SingleOrDefault(n => n.NodeId == oldLastNodeId);
        var newLastNode = nodeLocksForAgv.SingleOrDefault(n => n.NodeId == newLastNodeId);

        if (oldLastNode != null && oldLastNode.IsOccupiedByAgv(agvCorrelationId))
        {
            await nodeStateRepository.UnOccupyNodeAsync(agvCorrelationId);
            await PublishNodeStateChange(context, oldLastNodeId!);
            nodeLocksForAgv.Remove(oldLastNode);
        }

        if (newLastNode != null && newLastNode.IsReservedForAgv(agvCorrelationId))
        {
            await nodeStateRepository.OccupyNodeAsync(newLastNodeId!, agvCorrelationId);
            await PublishNodeStateChange(context, newLastNodeId!);
            nodeLocksForAgv.Remove(newLastNode);
        }

        if (nodeLocksForAgv.IsNullOrEmpty())
        {
            return;
        }

        await FreeUnusedNodes(context, nodeLocksForAgv, agvCorrelationId);
    }

    private async Task FreeUnusedNodes(IPublishEndpoint context, ICollection<NodeLock> nodeLocks, Guid agvCorrelationId)
    {
        ICollection<ActionGroup> agvActionGroups = await actionGroupRepository.GetPendingActionGroupsForAgvAsync(agvCorrelationId);
        ICollection<Task<bool>> freeNodeTasks = [];
        ICollection<Task> publishTasks = [];
        foreach (var node in nodeLocks)
        {
            if (!agvActionGroups.Any(a => a.Actions.Any(action => action.Target.NodeId == node.NodeId)))
            {
                freeNodeTasks.Add(nodeStateRepository.FreeNodeAsync(node.NodeId));
                publishTasks.Add(PublishNodeStateChange(context, node.NodeId));
            }
        }
        await Task.WhenAll(freeNodeTasks);
        await Task.WhenAll(publishTasks);
    }

    private async Task PublishNodeStateChange(IPublishEndpoint context, string nodeId)
    {
        var nodeState = await nodeStateRepository.GetNodeStateAsync(nodeId);
        var station = await stationRepository.GetStation(nodeId);
        if (nodeState != null)
        {
            await context.Publish(new NodeStateChanged(nodeState, station.Id));
            return;
        }
        await context.Publish(new NodeStateChanged(new NodeLock(nodeId, NodeLockState.Free, false, null, DateTime.UtcNow), station.Id));
    }
}