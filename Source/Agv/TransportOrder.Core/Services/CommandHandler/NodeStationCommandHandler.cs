using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.Graphs;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Stations;
using Gebhardt.Acc.Common.Messages.Stations.Requests;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for node and station related commands.
/// </summary>
[UsedImplicitly]
public class NodeStationCommandHandler(
    INodeRepository nodeRepository,
    IStationRepository stationRepository,
    IGraphRepository graphRepository,
    IActionGroupRepository actionGroupRepository,
    IMapRepository mapRepository,
    TimeProvider timeProvider,
    IAgvDb agvDb,
    ILogger<NodeStationCommandHandler> logger,
    ILoggingService loggingService) :
    IConsumer<FreeNode>,
    IConsumer<UpdateNodeOccupancy>,
    IConsumer<ClearNodesFromAgv>,
    IConsumer<SetNodeApproachingProperties>,
    IConsumer<InitializeNodeApproachingProperties>,
    IConsumer<FreeStationOccupancy>,
    IConsumer<OccupyStation>,
    IConsumer<UnblockStation>,
    IConsumer<ReserveStation>,
    IConsumer<LockStation>,
    IConsumer<UnlockStation>,
    IConsumer<UpdateStationJamState>,
    IConsumer<GetLockedStations>,
    IConsumer<StationConfigurationReceived>,
    IConsumer<UpdateStationAdditionalInfo>,
    IConsumer<Common.Messages.Maps.MapSaved>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<FreeNode> context)
    {
        var nodeId = context.Message.NodeId;
        bool reservedOnly = context.Message.ReservedOnly;
        var node = await nodeRepository.GetNodeAsync(nodeId);
        if (node.Lock == null)
        {
            logger.LogWarning("Could not retrieve node state for node {NodeId}", nodeId);
            return;
        }

        if (reservedOnly && node.Lock.NodeState != NodeLockState.Reserved)
        {
            logger.LogWarning("Node {NodeId} not reserved, but free is restricted to reserved nodes", nodeId);
            return;
        }

        node.Free();
        await nodeRepository.UpdateLock(node);
        await PublishNodeStateChange(context, node);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ClearNodesFromAgv> context)
    {
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var agv = await agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        var settings = agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
        await Task.Delay(settings.OfflineTolerance);
        ICollection<GraphNode> nodesForAgv = await nodeRepository.GetNodesForAgv(agvCorrelationId);
        ICollection<Task> tasks = [];
        foreach (var node in nodesForAgv.Where(n => !n.IsCharger() && !n.IsParker()))
        {
            tasks.Add(Task.Run(async () =>
            {
                node.Free();
                await nodeRepository.UpdateLock(node);
                await PublishNodeStateChange(context, node);
            }));
        }
        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UpdateNodeOccupancy> context)
    {
        (var agvCorrelationId, Guid? oldLastNodeId, Guid? newLastNodeId) = context.Message;
        logger.LogTrace("{MessageType} for AGV {CorrelationId} received", nameof(UpdateNodeOccupancy), agvCorrelationId);

        ICollection<GraphNode> nodes = await nodeRepository.GetNodesForAgv(agvCorrelationId);
        var oldLastNode = nodes.SingleOrDefault(n => n.Id == oldLastNodeId);
        var newLastNode = nodes.SingleOrDefault(n => n.Id == newLastNodeId);

        if (oldLastNode?.Lock != null && oldLastNode.Lock.IsOccupiedByAgv(agvCorrelationId))
        {
            var currentAction = await actionGroupRepository.GetCurrentAgvActionAsync(agvCorrelationId);
            if (currentAction?.Target.Node?.Id == oldLastNodeId)
            {
                logger.LogInformation("Node {NodeId} is occupied by AGV {AgvCorrelationId} and has an active action going towards it, this might be a redock, so not unoccupying it", oldLastNode, agvCorrelationId);
            }
            else
            {
                logger.LogInformation("UnOccupy node {NodeId}, occupied by agv {AgvCorrelationId}, new last node id is {NewLastNodeId}", oldLastNode, agvCorrelationId, newLastNode);

                oldLastNode.Free();
                await nodeRepository.UpdateLock(oldLastNode);
                await PublishNodeStateChange(context, oldLastNode);
                await context.Publish(new AgvLeftStation(oldLastNodeId.GetValueOrDefault(), agvCorrelationId));
                nodes.Remove(oldLastNode);
            }
        }

        if (newLastNode != null)
        {
            if (newLastNode.Lock != null && newLastNode.Lock.IsReservedForAgv(agvCorrelationId))
            {
                logger.LogWarning("Setting {NodeToLock} occupied by AGV {AgvCorrelationId}", newLastNode, agvCorrelationId);
                newLastNode.Occupy(agvCorrelationId, ignoreLocked: true);
                await nodeRepository.UpdateLock(newLastNode, ignoreLocked: true);
                await PublishNodeStateChange(context, newLastNode);
                nodes.Remove(newLastNode);
            }
            if (newLastNode.Lock != null && newLastNode.Lock.IsOccupiedByAgv(agvCorrelationId))
            {
                logger.LogWarning("{NodeToLock} already occupied by AGV {AgvCorrelationId}", newLastNode, agvCorrelationId);
                nodes.Remove(newLastNode);
            }
        }

        if (nodes.IsNullOrEmpty())
        {
            return;
        }

        await FreeUnusedNodes(context, nodes, agvCorrelationId);
    }

    private async Task FreeUnusedNodes(IPublishEndpoint context, ICollection<GraphNode> nodes, Guid agvCorrelationId)
    {
        ICollection<ActionGroup> agvActionGroups = await actionGroupRepository.GetPendingActionGroupsForAgvAsync(agvCorrelationId);
        ICollection<Task> tasks = [];
        foreach (var node in nodes.Where(l => l.Lock != null && l.Lock.ModifiedDate < timeProvider.GetUtcNow().AddMinutes(-1)))
        {
            if (!agvActionGroups.Any(a => a.Actions.Any(action => action.Target.Node?.Id == node.Id)))
            {
                logger.LogWarning("For agv {AgvCorrelationId} free residual node {NodeId}", agvCorrelationId, node.Id);

                tasks.Add(Task.Run(async () =>
                {
                    node.Free();
                    await nodeRepository.UpdateLock(node);
                    await PublishNodeStateChange(context, node);
                    await context.Publish(new FreeStationOccupancy(node.Id, agvCorrelationId));
                }));
            }
        }
        await Task.WhenAll(tasks);
    }

    private async Task PublishNodeStateChange(IPublishEndpoint context, GraphNode node)
    {
        Guid? stationId;
        string? user;
        string? lockReason;
        string? lockDescription;

        var station = await stationRepository.GetRequiredStationBaseInformation(node.Id);
        if (station != null)
        {
            stationId = station.Id;
            user = station.LockedBy;
            lockReason = station.LockReason;
            lockDescription = station.LockDescription;
            if (station.NodeLock == null && node.Lock != null)
            {
                station.NodeLock = node.Lock;
                await stationRepository.Update(station);
            }
        }
        else
        {
            stationId = null;
            user = null;
            lockReason = null;
            lockDescription = null;
        }

        if (node.Lock != null)
        {
            await context.Publish(new NodeStateChanged(node.Lock, node.Name, stationId, user, lockReason, lockDescription));
            return;
        }
        var dateModified = DateTime.UtcNow;
        await context.Publish(new NodeStateChanged(new NodeLock(node, NodeLockState.Free, false, null, dateModified), node.Name, stationId, user, lockReason, lockDescription));
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<FreeStationOccupancy> context)
    {
        var nodeId = context.Message.NodeId;
        var node = await nodeRepository.GetNodeAsync(nodeId);
        if (node.Lock == null)
        {
            logger.LogWarning("Could not retrieve node state for station with Node {NodeId}", nodeId);
            return;
        }

        if (!node.Free())
        {
            logger.LogWarning("Could not free node {NodeId}, node is locked", nodeId);
        }
        await nodeRepository.UpdateLock(node);
        await PublishNodeStateChange(context, node);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UnblockStation> context)
    {
        var stationId = context.Message.StationId;
        var station = await stationRepository.GetStation(stationId);
        var node = await nodeRepository.GetNodeAsync(stationId);
        if (node.Lock == null)
        {
            logger.LogWarning("Could not retrieve node state for station {StationName}", stationId);
            return;
        }
        Guid? agvCorrelationId = node.Lock.AgvCorrelationId;

        if (!node.Free())
        {
            logger.LogWarning("Could not unblock node {NodeId}, node is locked", stationId);
        }
        await nodeRepository.UpdateLock(node);
        await context.Publish(new AgvLeftStation(station.Id, agvCorrelationId.GetValueOrDefault()));
        await PublishNodeStateChange(context, node);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<OccupyStation> context)
    {
        var agv = context.Message.AgvCorrelationId;
        var station = await stationRepository.GetStation(context.Message.Station.Id);
        var node = await nodeRepository.GetNodeAsync(station.Id);
        node.Occupy(agv, true, ignoreLocked: true);
        await nodeRepository.UpdateLock(node, ignoreLocked: true);
        await PublishNodeStateChange(context, node);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ReserveStation> context)
    {
        (var station, var agv, bool ignoreStateLocked) = context.Message;

        var node = await nodeRepository.GetNodeAsync(station.Id);
        node.Reserve(agv, true, ignoreLocked: ignoreStateLocked);
        await nodeRepository.UpdateLock(node, ignoreLocked: ignoreStateLocked);
        await PublishNodeStateChange(context, node);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<LockStation> context)
    {
        (var id, string user, string reason, string lockDescription, var lockOrderHandling) = context.Message;

        var station = await stationRepository.GetStation(id);
        var node = await nodeRepository.GetNodeAsync(id);
        bool lockResult = node.Lock();
        if (!lockResult)
        {
            logger.LogError("Lock of station {StationName} failed ", station.Name);
            return;
        }

        station.NodeLock = node.Lock;
        await nodeRepository.UpdateLock(node);

        station.Lock(user, reason, lockDescription, lockOrderHandling);

        await stationRepository.Update(station);

        await loggingService.LogStation(logger, LogLevel.Information, LogCodes.StationLocked, id, "Station Locked",
            $"User {user} locked station because {reason}",
            new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Reason, reason } });
        await context.Publish(new StationLocked(station.Name));

        if (node.Lock != null)
        {
            await context.Publish(new NodeStateChanged(node.Lock, node.Name, station.Id, station.LockedBy, station.LockReason, station.LockDescription));
        }
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UnlockStation> context)
    {
        (var id, string user) = context.Message;

        var station = await stationRepository.GetStation(id);
        var node = await nodeRepository.GetNodeAsync(id);
        bool unlockResult = node.Unlock();

        if (!unlockResult)
        {
            logger.LogError("Unlock of station {StationName} failed ", station.Name);
            return;
        }

        await nodeRepository.UpdateLock(node);

        station.Unlock();
        station.NodeLock = node.Lock;
        await stationRepository.Update(station);

        if (node.Lock != null)
        {
            await context.Publish(new NodeStateChanged(node.Lock, node.Name, station.Id, user, station.LockReason, station.LockDescription));
        }

        await loggingService.LogStation(logger, LogLevel.Information, LogCodes.StationUnlocked, id, "Station Unlocked",
            $"User {user} unlocked station",
            new Dictionary<string, object?> { { LogLabels.User, user } });

        await context.Publish(new StationUnlocked(node));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UpdateStationJamState> context)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var station = await stationRepository.GetStation(context.Message.CorrelationId);

        //publish first to transfer old and new state
        await context.Publish(new JamStateChanged(station.Id, context.Message.HasPalletJam, station.PalletJamChangedDate, station.HasPalletJam, now));
        station.HasPalletJam = context.Message.HasPalletJam;
        station.PalletJamChangedDate = now;
        await stationRepository.Update(station);
        logger.LogInformation("Jam state of station {StationName} ({StationId}) updated to {JamState}", station.Name, station.Id, station.HasPalletJam);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetLockedStations> context)
    {
        ICollection<Station> stations = await stationRepository.GetStations();

        await context.RespondAsync(new GotLockedStations(stations.Where(s => s.IsLocked).Select(s => s.Name).ToList()));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<StationConfigurationReceived> context)
    {
        var (stationId, stationConfiguration) = context.Message;

        var station = await stationRepository.GetStation(stationId);
        station.Configure(stationConfiguration);
        await stationRepository.Update(station);

        logger.LogInformation("Configured station {Station}", station.Name);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UpdateStationAdditionalInfo> context)
    {
        (var stationId, Dictionary<string, object?> additionalInformation) = context.Message;
        var station = await stationRepository.GetStation(stationId);
        station.UpdateAdditionalInfo(additionalInformation);
        await stationRepository.Update(station);

        logger.LogInformation("Updated additional information of station {Station}", station.Name);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<Common.Messages.Maps.MapSaved> context)
    {
        var agvMap = context.Message.Map;
        ICollection<Guid> agvIds = await graphRepository.GetAgvIdsRelatedToNodesAsync(agvMap.Nodes);
        if (agvIds.Count == 0)
        {
            logger.LogDebug("No agvs assigned to map, do not update stations");
            return;
        }
        var agvGraph = await graphRepository.GetAgvGraphAsync(agvIds.First());
        await stationRepository.AddStations(agvGraph);
        logger.LogDebug("Stations updated");
    }
}