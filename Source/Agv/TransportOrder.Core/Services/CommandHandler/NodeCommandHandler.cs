using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.Graphs;
using Gebhardt.Acc.Common.Messages.Stations;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
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
    IMapRepository mapRepository,
    TimeProvider timeProvider,
    IAgvDb agvDb,
    ILogger<NodeCommandHandler> logger
) :
    IConsumer<FreeNode>,
    IConsumer<UpdateNodeOccupancy>,
    IConsumer<ClearNodesFromAgv>,
    IConsumer<SetNodeApproachingProperties>,
    IConsumer<InitializeNodeApproachingProperties>,
    IConsumer<FreeStationOccupancy>,
    IConsumer<OccupyStation>,
    IConsumer<UnblockStation>,
    IConsumer<StationReserved>
{
    /// <summary>
    /// Handles the <see cref="FreeNode"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FreeNode> context)
    {
        var nodeId = context.Message.NodeId;
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
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var agv = await agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        var settings = agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
        await Task.Delay(settings.OfflineTolerance);
        ICollection<NodeLock> nodeLocksForAgv = await nodeStateRepository.GetNodeLocksForAgv(agvCorrelationId);
        ICollection<GraphNode> nodes = await agvDb.Graphs.GetNodesByIdsAsync(nodeLocksForAgv.Select(n => n.NodeId).ToList());
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
        (var agvCorrelationId, Guid? oldLastNodeId, Guid? newLastNodeId) = context.Message;
        logger.LogTrace("{MessageType} for AGV {CorrelationId} received", nameof(UpdateNodeOccupancy), agvCorrelationId);

        ICollection<NodeLock> nodeLocksForAgv = await nodeStateRepository.GetNodeLocksForAgv(agvCorrelationId);
        var oldLastNode = nodeLocksForAgv.SingleOrDefault(n => n.NodeId == oldLastNodeId);
        var newLastNode = nodeLocksForAgv.SingleOrDefault(n => n.NodeId == newLastNodeId);

        if (oldLastNode != null && oldLastNode.IsOccupiedByAgv(agvCorrelationId))
        {
            var currentAction = await actionGroupRepository.GetCurrentAgvActionAsync(agvCorrelationId);
            if (currentAction?.Target.Node?.Id == oldLastNodeId)
            {
                logger.LogInformation("Node {NodeId} is occupied by AGV {AgvCorrelationId} and has an active action going towards it, this might be a redock, so not unoccupying it", oldLastNode, agvCorrelationId);
            }
            else
            {
                logger.LogInformation("UnOccupy node {NodeId}, occupied by agv {AgvCorrelationId}, new last node id is {NewLastNodeId}", oldLastNode, agvCorrelationId, newLastNode);
                await nodeStateRepository.UnOccupyNodeAsync(agvCorrelationId);
                await PublishNodeStateChange(context, oldLastNodeId.GetValueOrDefault());
                await context.Publish(new AgvLeftStation(oldLastNodeId.GetValueOrDefault(), agvCorrelationId));
                nodeLocksForAgv.Remove(oldLastNode);
            }
        }

        if (newLastNode != null)
        {
            if (newLastNode.IsReservedForAgv(agvCorrelationId))
            {
                logger.LogWarning("Setting {NodeToLock} occupied by AGV {AgvCorrelationId}", newLastNode, agvCorrelationId);
                await nodeStateRepository.OccupyNodeAsync(newLastNodeId!.Value, agvCorrelationId);
                await PublishNodeStateChange(context, newLastNodeId.Value);
                nodeLocksForAgv.Remove(newLastNode);
            }
            if (newLastNode.IsOccupiedByAgv(agvCorrelationId))
            {
                logger.LogWarning("{NodeToLock} already occupied by AGV {AgvCorrelationId}", newLastNode, agvCorrelationId);
                nodeLocksForAgv.Remove(newLastNode);
            }
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
        foreach (var node in nodeLocks.Where(l => l.ModifiedDate < timeProvider.GetUtcNow().AddMinutes(-1)))
        {
            if (!agvActionGroups.Any(a => a.Actions.Any(action => action.Target.Node?.Id == node.NodeId)))
            {
                logger.LogWarning("For agv {AgvCorrelationId} free residual node {NodeId}", agvCorrelationId, node.NodeId);

                tasks.Add(Task.Run(async () =>
                {
                    await nodeStateRepository.FreeNodeAsync(node.NodeId);
                    await PublishNodeStateChange(context, node.NodeId);
                    await context.Publish(new FreeStationOccupancy(node.NodeId, agvCorrelationId));
                }));
            }
        }
        await Task.WhenAll(tasks);
    }

    private async Task PublishNodeStateChange(IPublishEndpoint context, Guid nodeId)
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
        var node = await agvDb.Graphs.GetNodeAsync(nodeId);
        await context.Publish(new NodeStateChanged(new NodeLock(node, NodeLockState.Free, false, null, dateModified), stationId, user, lockReason, lockDescription));
    }

    /// <summary>
    /// Handles the <see cref="SetNodeApproachingProperties"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SetNodeApproachingProperties> context)
    {
        (var mapId, var nodeId, double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection) = context.Message;

        var node = await agvDb.Graphs.GetNodeAsync(nodeId);
        dockingDirection ??= DockingDirection.Both;

        logger.LogInformation("Setting approaching properties for node {NodeId} on map {MapId}: docking direction {DockingDirection}, drop lift height {DropLiftHeight}, pick lift height {PickLiftHeight}", node, mapId, dockingDirection, dropLiftHeight, pickLiftHeight);

        node.DockingDirection = dockingDirection;
        node.DropLiftHeight = dropLiftHeight;
        node.PickLiftHeight = pickLiftHeight;

        await agvDb.Graphs.UpdateNode(node);

        await context.Publish(new NodeApproachingPropertiesUpdated(mapId, node, pickLiftHeight, dropLiftHeight, dockingDirection));
    }

    /// <summary>
    /// Handles the <see cref="InitializeNodeApproachingProperties"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<InitializeNodeApproachingProperties> context)
    {
        (var nodeId, double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection) = context.Message;

        var node = await agvDb.Graphs.GetNodeAsync(nodeId);

        node.DockingDirection = dockingDirection;
        node.DropLiftHeight = dropLiftHeight;
        node.PickLiftHeight = pickLiftHeight;

        await agvDb.Graphs.UpdateNode(node);

        var mapId = await mapRepository.GetMapIdForNodeAsync(nodeId);

        await context.Publish(new NodeApproachingPropertiesUpdated(mapId, node, pickLiftHeight, dropLiftHeight, dockingDirection));
    }

    /// <summary>
    /// Handles the <see cref="FreeStationOccupancy"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FreeStationOccupancy> context)
    {
        var nodeId = context.Message.NodeId;
        var nodeState = await nodeStateRepository.GetNodeStateAsync(nodeId);
        if (nodeState == null)
        {
            logger.LogWarning("Could not retrieve node state for station with Node {NodeId}", nodeId);
            return;
        }

        if (!await nodeStateRepository.FreeNodeAsync(nodeId))
        {
            logger.LogWarning("Could not free node {NodeId}, node is locked", nodeId);
        }

        await PublishNodeStateChange(context, nodeId);
    }

    /// <summary>
    /// Handles the <see cref="UnblockStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UnblockStation> context)
    {
        var stationId = context.Message.StationId;
        var station = await stationRepository.GetStation(stationId);
        var nodeState = station.NodeLock;
        if (nodeState == null)
        {
            logger.LogWarning("Could not retrieve node state for station {StationName}", stationId);
            return;
        }
        Guid? agvCorrelationId = nodeState.AgvCorrelationId;
        if (!await nodeStateRepository.FreeNodeAsync(stationId))
        {
            logger.LogWarning("Could not unblock node {NodeId}, node is locked", stationId);
        }
        await context.Publish(new AgvLeftStation(station.Id, agvCorrelationId.GetValueOrDefault()));
        await PublishNodeStateChange(context, stationId);
    }

    /// <summary>
    /// Handles the <see cref="OccupyStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<OccupyStation> context)
    {
        var station = context.Message.Station;
        var agv = context.Message.AgvCorrelationId;

        await nodeStateRepository.ForceReserveNodeAsync(station.Id, agv);
        await PublishNodeStateChange(context, station.Id);
    }

    /// <summary>
    /// Handles the <see cref="StationReserved"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StationReserved> context)
    {
        var stationId = context.Message.StationId;
        var agv = context.Message.AgvCorrelationId;
        await nodeStateRepository.ForceReserveNodeAsync(stationId, agv);
        await PublishNodeStateChange(context, stationId);
    }
}