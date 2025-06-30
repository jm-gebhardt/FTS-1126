using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
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
    IGraphRepository graphRepository,
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
        bool reservedOnly = context.Message.ReservedOnly;
        var station = await stationRepository.GetStation(nodeId);
        var nodeState = await nodeStateRepository.GetNodeStateAsync(nodeId);
        if (nodeState == null)
        {
            logger.LogWarning("Could not retrieve node state for node {NodeId}", nodeId);
            return;
        }

        if (reservedOnly && nodeState.NodeState != NodeLockState.Reserved)
        {
            logger.LogWarning("Node {NodeId} not reserved, but free is restricted to reserved nodes", nodeId);
            return;
        }

        if (!await nodeStateRepository.FreeNodeAsync(nodeId))
        {
            logger.LogWarning("Could not free node {NodeId}, node is locked", nodeId);
        }

        nodeState = await nodeStateRepository.GetNodeStateAsync(nodeId);
        await context.Publish(new NodeStateChanged(nodeState!, station.Id, station.LockedBy, station.LockReason, station.LockDescription));
    }

    /// <summary>
    /// Handles the <see cref="ClearNodesFromAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ClearNodesFromAgv> context)
    {
        await Task.Delay(Agv.OfflineTolerance);
        ICollection<NodeLock> nodeLocksForAgv = await nodeStateRepository.GetNodeLocksForAgv(context.Message.AgvCorrelationId);
        ICollection<GraphNode> nodes = await graphRepository.GetNodesByIdsAsync(nodeLocksForAgv.Select(n => n.NodeId).ToList());
        ICollection<Task> tasks = [];
        foreach (var node in nodes.Where(n => !n.IsCharger() && !n.IsParker()))
        {
            tasks.Add(Task.Run(async () =>
            {
                await nodeStateRepository.FreeNodeAsync(node.Id);
                await PublishNodeStateChange(context, node.Id);
            }));
        }
        await Task.WhenAll(tasks);
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
            var currentAction = await actionGroupRepository.GetCurrentAgvActionAsync(agvCorrelationId);
            if (currentAction?.Target.NodeId == oldLastNodeId)
            {
                logger.LogInformation("Node {NodeId} is occupied by AGV {AgvCorrelationId} and has an active action going towards it, this might be a redock, so not unoccupying it", oldLastNode, agvCorrelationId);
            }
            else
            {
                logger.LogInformation("UnOccupy node {NodeId}, occupied by agv {AgvCorrelationId}, new last node id is {NewLastNodeId}", oldLastNode, agvCorrelationId, newLastNode);
                await nodeStateRepository.UnOccupyNodeAsync(agvCorrelationId);
                await PublishNodeStateChange(context, oldLastNodeId!);
                nodeLocksForAgv.Remove(oldLastNode);
            }
        }

        if (newLastNode != null && newLastNode.IsReservedForAgv(agvCorrelationId))
        {
            logger.LogWarning("Setting {NodeToLock} occupied by AGV {AgvCorrelationId}", newLastNode, agvCorrelationId);
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
        ICollection<Task> tasks = [];
        foreach (var node in nodeLocks)
        {
            if (!agvActionGroups.Any(a => a.Actions.Any(action => action.Target.NodeId == node.NodeId)))
            {
                logger.LogWarning("For agv {AgvCorrelationId} free residual node {NodeId}", agvCorrelationId, node.NodeId);

                tasks.Add(Task.Run(async () =>
                {
                    await nodeStateRepository.FreeNodeAsync(node.NodeId);
                    await PublishNodeStateChange(context, node.NodeId);
                }));
            }
        }
        await Task.WhenAll(tasks);
    }

    private async Task PublishNodeStateChange(IPublishEndpoint context, string nodeId)
    {
        var nodeState = await nodeStateRepository.GetNodeStateAsync(nodeId);
        Guid? stationId;
        string? user;
        string? lockReason;
        string? lockDescription;

        try
        {
            var station = await stationRepository.GetStation(nodeId);
            stationId = station.Id;
            user = station.LockedBy;
            lockReason = station.LockReason;
            lockDescription = station.LockDescription;
        }
        catch (NodeNotFoundException)
        {
            stationId = null;
            user = null;
            lockReason = null;
            lockDescription = null;
        }

        if (nodeState != null)
        {
            await context.Publish(new NodeStateChanged(nodeState, stationId, user, lockReason, lockDescription));
            return;
        }
        var dateModified = DateTime.UtcNow;
        await context.Publish(new NodeStateChanged(new NodeLock(nodeId, NodeLockState.Free, false, null, dateModified), stationId, user, lockReason, lockDescription));
    }
}