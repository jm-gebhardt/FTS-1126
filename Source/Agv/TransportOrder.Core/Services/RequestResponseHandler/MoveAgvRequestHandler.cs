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
using System.Globalization;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers to (manually) move an agv.
/// </summary>
/// <inheritdoc cref="MoveAgvRequestHandler"/>>
[UsedImplicitly]
public class MoveAgvRequestHandler :
    IConsumer<MoveAgv>,
    IConsumer<ChargeAgv>,
    IConsumer<MoveAgvToCoordinate>
{
    private readonly ILogger<MoveAgvRequestHandler> _logger;
    private readonly IAgvDb _agvDb;
    private readonly IBus _bus;
    private readonly INodeRepository _nodeRepository;
    private readonly IParkPointService _parkPointService;
    private readonly IChargingPointManager _chargingPointManager;
    private readonly IRestrictedAreaRepository _restrictedAreaRepository;
    private readonly IStationRepository _stationRepository;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Provides handlers to (manually) move an agv.
    /// </summary>
    /// <inheritdoc cref="MoveAgvRequestHandler"/>>
    public MoveAgvRequestHandler(ILogger<MoveAgvRequestHandler> logger,
        IAgvDb agvDb,
        IBus bus,
        INodeRepository nodeRepository,
        IParkPointService parkPointService,
        IChargingPointManager chargingPointManager,
        IRestrictedAreaRepository restrictedAreaRepository,
        IStationRepository stationRepository,
        ILoggingService loggingService)
    {
        _logger = logger;
        _agvDb = agvDb;
        _bus = bus;
        _nodeRepository = nodeRepository;
        _parkPointService = parkPointService;
        _chargingPointManager = chargingPointManager;
        _restrictedAreaRepository = restrictedAreaRepository;
        _stationRepository = stationRepository;
        _loggingService = loggingService;
    }

    /// <summary>
    /// Handles the <see cref="MoveAgv"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<MoveAgv> context)
    {
        (var agvCorrelationId, var targetNodeId, var creator, string user, double theta) = context.Message;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        if (!agv.CanStartManualOperationalAction)
        {
            throw new ActionNotAllowedInModeException(agv.CorrelationId, agv.Mode.Type.ToString());
        }

        GraphNode? targetNode;
        try
        {
            targetNode = await _agvDb.Graphs.GetNodeAsync(targetNodeId);
        }
        catch (NodeNotFoundException ex)
        {
            throw new ValidationException(ex.Message);
        }

        if (targetNode.NeedsReservation())
        {
            await ReserveNodeForAgv(targetNode.GetReservableNodeTypes().First(), targetNode, agvCorrelationId);
        }

        var rotationInfo = new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.CargoRotation, theta.ToString(CultureInfo.InvariantCulture) } };

        var actionGroupId = Guid.NewGuid();
        await QueueNewActionGroup(context,
            [
                new AgvAction(Guid.NewGuid(),
                    ActionType.Move,
                    targetNode,
                    actionGroupId,
                    rotationInfo)
            ],
            $"Manual {ActionType.Move}",
            agvCorrelationId,
            creator,
            actionGroupId);
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, agvCorrelationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvManualMove } });

        await context.RespondAsync(new AgvMoveSuccessful(actionGroupId));
    }

    /// <summary>
    /// Handles the <see cref="ChargeAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ChargeAgv> context)
    {
        (var agvCorrelationId, var chargerId, string user) = context.Message;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        if (!agv.CanStartManualOperationalAction)
        {
            throw new ActionNotAllowedInModeException(agv.CorrelationId, agv.Mode.Type.ToString());
        }

        GraphNode? node;

        try
        {
            node = await _agvDb.Graphs.GetNodeAsync(chargerId);
        }
        catch (NodeNotFoundException ex)
        {
            throw new ValidationException(ex.Message);
        }

        if (!node.IsCharger())
        {
            throw new InvalidNodeTypeException(node.Name, node.TypeString(), nameof(AgvGraph.NodeType.Charge));
        }

        if (agv.AgvStatus?.LastNodeId != chargerId)
        {
            await ReserveNodeForAgv(AgvGraph.NodeType.Charge, node, agvCorrelationId);
        }

        var actionGroupId = Guid.NewGuid();
        await QueueNewActionGroup(context,
            [
                new AgvAction(Guid.NewGuid(), ActionType.Move, node, actionGroupId)
                {
                    Sequence = 0
                },

                new AgvAction(Guid.NewGuid(), ActionType.Charge, node, actionGroupId)
                {
                    Sequence = 1
                }
            ], $"Manual {nameof(ActionType.Charge)}",
            agvCorrelationId,
            ActionGroupCreator.TransportOrderWebApi,
            actionGroupId, priority: Priority.Charging);
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, agvCorrelationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvManualCharge } });

        await context.RespondAsync(new AgvMoveSuccessful(actionGroupId));
    }

    /// <summary>
    /// Handles the <see cref="MoveAgvToCoordinate"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<MoveAgvToCoordinate> context)
    {
        (var agvCorrelationId, float x, float y, float theta, var creator, string user) = context.Message;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        if (!agv.ShouldStartManualAction)
        {
            throw new ActionNotAllowedInModeException(agv.CorrelationId, agv.Mode.Type.ToString());
        }

        if (await _restrictedAreaRepository.IsPointInRestrictedArea(x, y))
        {
            throw new PositionIsInRestrictedAreaException(x, y);
        }

        var actionGroupId = Guid.NewGuid();
        await QueueNewActionGroup(context,
            [
                new AgvAction(Guid.NewGuid(),
                    ActionType.Move,
                    new ActionTarget(x, y, theta),
                    actionGroupId)
            ],
            $"Manual {ActionType.Move} to ({x}, {y}) with orientation {theta}",
            agvCorrelationId,
            creator,
            actionGroupId);
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, agvCorrelationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvManualMove } });

        await context.RespondAsync(new AgvMoveSuccessful(actionGroupId));
    }

    private async Task ReserveNodeForAgv(AgvGraph.NodeType type, GraphNode targetNode, Guid agvCorrelationId)
    {
        switch (type)
        {
            case AgvGraph.NodeType.Charge:
                await _chargingPointManager.ReserveChargerAsync(targetNode, agvCorrelationId, true);
                break;

            case AgvGraph.NodeType.Park:
                await _parkPointService.ReserveParkAsync(targetNode.Id, agvCorrelationId, true);
                break;

            default:
                throw new ArgumentOutOfRangeException($"Unable to reserve node type: {targetNode.Type}");
        }

        await PublishNodeStateChange(targetNode.Id);
    }

    private async Task QueueNewActionGroup(ConsumeContext context, ICollection<AgvAction> actions, string orderId, Guid agvCorrelationId, ActionGroupCreator creator, Guid actionGroupId, Priority priority = Priority.Manual)
    {
        var actionGroup = new ActionGroup(
            actionGroupId,
            orderId,
            actions,
            assignedAgv: agvCorrelationId,
            isManual: true,
            priority: priority,
            shouldEscapeQueue: true,
            creator: creator
        );
        IRequestClient<QueueActionGroup>? requestClient = context.CreateRequestClient<QueueActionGroup>(_bus);
        await requestClient.GetResponse<Success>(new QueueActionGroup(actionGroup, true));
    }

    private async Task PublishNodeStateChange(Guid nodeId)
    {
        var node = await _nodeRepository.GetNodeAsync(nodeId);
        var station = await _stationRepository.GetStation(nodeId);
        if (node.Lock == null)
        {
            return;
        }
        await _bus.Publish(new NodeStateChanged(node.Lock!, node.Name, station.Id, station.LockedBy, station.LockReason, station.LockDescription));
    }
}