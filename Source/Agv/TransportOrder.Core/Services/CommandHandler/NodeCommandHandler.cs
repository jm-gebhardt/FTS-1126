using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.Graphs;
using Gebhardt.Acc.Common.Messages.Stations;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

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
    IMapRepository mapRepository,
    ILogger<NodeCommandHandler> logger) :
    IConsumer<FreeNode>,
    IConsumer<UpdateNodeOccupancy>,
    IConsumer<ClearNodesFromAgv>,
    IConsumer<SetNodeApproachingProperties>,
    IConsumer<InitializeNodeApproachingProperties>,
    IConsumer<FreeStation>, 
    IConsumer<OccupyStation>,
    IConsumer<UnblockStation>,
    IConsumer<StationReserved>
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
                var node = await graphRepository.GetNodeAsync(oldLastNodeId!);
                if (node.IsDropStation() || node.IsPickStation())
                {
                    logger.LogInformation("Not un-occupying station {Station} based on last node id, only explicit freeing via command", oldLastNodeId);
                    return;
                }
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

    /// <summary>
    /// Handles the <see cref="SetNodeApproachingProperties"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SetNodeApproachingProperties> context)
    {
        (var mapId, string nodeId, double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection) = context.Message;

        var node = await graphRepository.GetNodeAsync(nodeId);
        dockingDirection ??= DockingDirection.Both;
        
        logger.LogInformation("Setting approaching properties for node {NodeId} on map {MapId}: docking direction {DockingDirection}, drop lift height {DropLiftHeight}, pick lift height {PickLiftHeight}", node, mapId, dockingDirection, dropLiftHeight, pickLiftHeight);
        
        node.DockingDirection = dockingDirection;
        node.DropLiftHeight = dropLiftHeight;
        node.PickLiftHeight = pickLiftHeight;

        await graphRepository.UpdateNode(node);

        await context.Publish(new NodeApproachingPropertiesUpdated(mapId, nodeId, pickLiftHeight, dropLiftHeight, dockingDirection));
    }

    /// <summary>
    /// Handles the <see cref="InitializeNodeApproachingProperties"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<InitializeNodeApproachingProperties> context)
    {
        (string nodeId, double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection) = context.Message;

        var node = await graphRepository.GetNodeAsync(nodeId);
           
        node.DockingDirection = dockingDirection;
        node.DropLiftHeight = dropLiftHeight;
        node.PickLiftHeight = pickLiftHeight;

        await graphRepository.UpdateNode(node);

        var mapId = await mapRepository.GetMapIdForNode(nodeId);

        await context.Publish(new NodeApproachingPropertiesUpdated(mapId, nodeId, pickLiftHeight, dropLiftHeight, dockingDirection));
    }

    /// <summary>
    /// Handles the <see cref="FreeStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FreeStation> context)
    {
        string stationName = context.Message.Station;
        var nodeState = await nodeStateRepository.GetNodeStateAsync(stationName);
        if (nodeState == null)
        {
            logger.LogWarning("Could not retrieve node state for station {StationName}", stationName);
            return;
        }

        if (!await nodeStateRepository.FreeNodeAsync(stationName))
        {
            logger.LogWarning("Could not free node {NodeId}, node is locked", stationName);
        }

        await PublishNodeStateChange(context, stationName);
    }

    /// <summary>
    /// Handles the <see cref="UnblockStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UnblockStation> context)
    {
        string stationName = context.Message.Station;
        var nodeState = await nodeStateRepository.GetNodeStateAsync(stationName);
        if (nodeState == null)
        {
            logger.LogWarning("Could not retrieve node state for station {StationName}", stationName);
            return;
        }

        if (!await nodeStateRepository.FreeNodeAsync(stationName))
        {
            logger.LogWarning("Could not unblock node {NodeId}, node is locked", stationName);
        }

        await PublishNodeStateChange(context, stationName);
    }

    /// <summary>
    /// Handles the <see cref="OccupyStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<OccupyStation> context)
    {
        string stationName = context.Message.Station;
        var agv = context.Message.AgvCorrelationId;

        await nodeStateRepository.ForceReserveNodeAsync(stationName, agv);
        await PublishNodeStateChange(context, stationName);
    }

    /// <summary>
    /// Handles the <see cref="StationReserved"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StationReserved> context)
    {
        string stationName = context.Message.StationName;
        var agv = context.Message.AgvCorrelationId;
        await nodeStateRepository.ForceReserveNodeAsync(stationName, agv);
        await PublishNodeStateChange(context, stationName);
    }
}