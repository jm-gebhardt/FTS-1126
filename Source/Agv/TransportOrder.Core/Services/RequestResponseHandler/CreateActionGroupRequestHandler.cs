using AutoMapper;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.Acc.Common.Messages.ActionGroups.ActionGroup;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for action group creation requests.
/// </summary>
[UsedImplicitly]
public class CreateActionGroupRequestHandler :
    IConsumer<CreatePickDropActionGroup>
{
    private readonly IActionGroupRetrievalService _retrievalService;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IAgvDb _agvDb;
    private readonly IMapper _mapper;
    private readonly IActionGroupValidationService _actionGroupValidationService;
    private readonly IBus _bus;
    private readonly ILogger<CreateActionGroupRequestHandler> _logger;
    private readonly ILoggingService _loggingService;

    /// <inheritdoc cref="CreateActionGroupRequestHandler" />
    public CreateActionGroupRequestHandler(IActionGroupRetrievalService retrievalService,
        IActionGroupRepository actionGroupRepository,
        IActionGroupValidationService actionGroupValidationService,
        IBus bus,
        IAgvDb agvDb,
        IMapper mapper,
        ILogger<CreateActionGroupRequestHandler> logger,
        ILoggingService loggingService
    )
    {
        _retrievalService = retrievalService;
        _actionGroupRepository = actionGroupRepository;
        _agvDb = agvDb;
        _mapper = mapper;
        _actionGroupValidationService = actionGroupValidationService;
        _loggingService = loggingService;
        _bus = bus;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="Messages.Requests.ActionGroup.CreatePickDropActionGroup"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<CreatePickDropActionGroup> context)
    {
        (var sourceNode, var targetNode, bool dockFrontPick, bool dockFrontDrop, string? handlingUnit, var agvCorrelationId, string? infoPick, string? infoDrop, var creator, string user) = await ValidateMessageContent(context.Message);

        var actionGroup = await CreatePickDropActionGroup(sourceNode, targetNode, dockFrontPick, dockFrontDrop, handlingUnit, agvCorrelationId, infoPick, infoDrop, creator);
        actionGroup.Queue();
        await _actionGroupRepository.AddAsync(actionGroup);

        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderCreated, agvCorrelationId.GetValueOrDefault(),
            actionGroup.CorrelationId, "Order Created", $"Order {actionGroup.OrderId} was created successfully",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });

        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderQueued, agvCorrelationId.GetValueOrDefault(),
            actionGroup.CorrelationId, "Order Queued", $"Order {actionGroup.OrderId} was queued successfully",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });

        await _bus.Publish(new Common.Messages.ActionGroups.Events.ActionGroupCreated(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId, agvCorrelationId));
        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderOperatorInteraction, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Operator Interaction", "Operator interacted with order", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.OrderCreated } });
        await context.RespondAsync(new ActionGroupCreated(await _retrievalService.EnrichActionGroupWithNodesAsync(actionGroup)));
    }

    private async Task<(GraphNode? sourceNode, GraphNode? targetNode, bool dockFrontPick, bool dockFrontDrop, string? handlingUnit, Guid? agvCorrelationId,
            string? infoPick, string? infoDrop, ActionGroupCreator creator, string user)>
        ValidateMessageContent(CreatePickDropActionGroup createPickDropActionGroup)
    {
        var (sourceNodeId, targetNodeId, _, _, _, agvCorrelationId, _, _, _, _) = createPickDropActionGroup;

        // validate agv existence 
        if (agvCorrelationId != null)
        {
            var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId.Value);
            if (!agv.CanStartManualPickDropAction && sourceNodeId != null && targetNodeId != null)
            {
                throw new ActionNotAllowedInModeException(agv.CorrelationId, agv.Mode.Type.ToString());
            }
        }

        // validate source and target node
        if (sourceNodeId == null && targetNodeId == null)
        {
            throw new ActionGroupInvalidException([Reasons.ActionGroupMustHaveTargetOrSource]);
        }

        if (agvCorrelationId == null && (sourceNodeId == null || targetNodeId == null))
        {
            throw new AgvNotAssignedToActionGroupException("Pick or drop only order requires assigned agv");
        }

        var (sourceNode, targetNode) = await TryGetSourceAndTargetNode(sourceNodeId, targetNodeId, agvCorrelationId);
        if (sourceNode != null && !sourceNode.IsPickStation())
        {
            throw new InvalidNodeTypeException(sourceNode.Name, sourceNode.Type.ToString(), $"{nameof(AgvGraph.NodeType.Pick)} or {nameof(AgvGraph.NodeType.PickDrop)}");
        }

        if (targetNode != null && !targetNode.IsDropStation())
        {
            throw new InvalidNodeTypeException(targetNode.Name, targetNode.Type.ToString(), $"{nameof(AgvGraph.NodeType.Drop)} or {nameof(AgvGraph.NodeType.PickDrop)}");
        }

        return (sourceNode, targetNode, createPickDropActionGroup.DockFrontPick, createPickDropActionGroup.DockFrontDrop,
            createPickDropActionGroup.HandlingUnit, agvCorrelationId, createPickDropActionGroup.AdditionalPickInfo,
            createPickDropActionGroup.AdditionalDropInfo, createPickDropActionGroup.Creator, createPickDropActionGroup.User);
    }

    private async Task<(GraphNode? sourceNode, GraphNode? targetNode)> TryGetSourceAndTargetNode(Guid? sourceNodeId, Guid? targetNodeId, Guid? agvCorrelationId)
    {
        var nodeIds = new List<Guid>();
        if (sourceNodeId != null)
        {
            nodeIds.Add(sourceNodeId.Value);
        }
        if (targetNodeId != null)
        {
            nodeIds.Add(targetNodeId.Value);
        }
        ICollection<GraphNode> nodes;
        if (agvCorrelationId == null)
        {
            // get source and target independent of agv graph 
            nodes = await _agvDb.Graphs.GetNodesByIdsAsync(nodeIds);
        }
        else
        {
            // get source and target from the graph of the given agv
            // Cast to IEnumerable to avoid C# 14 span overload resolution breaking change
            Guid?[] nodeIdArray = [sourceNodeId, targetNodeId];
            IEnumerable<Guid?> nodeIdsToFetch = nodeIdArray;
            nodes = (await _agvDb.Graphs.GetNodesAsync(agvCorrelationId.Value, node => nodeIdsToFetch.Contains(node.Id))).ToList();
        }
        try
        {
            return (sourceNodeId != null ? nodes.SelectById(sourceNodeId.Value) : null, targetNodeId != null ? nodes.SelectById(targetNodeId.Value) : null);
        }
        catch (NodeNotFoundException ex)
        {
            throw new ValidationException(ex.Message);
        }
    }

    private async Task<Domain.Models.ActionGroup.ActionGroup> CreatePickDropActionGroup(GraphNode? sourceNode, GraphNode? targetNode, bool dockFrontPick, bool dockFrontDrop, string? handlingUnit, Guid? agvCorrelationId, string? infoPick, string? infoDrop, ActionGroupCreator creator)
    {
        var correlationId = Guid.NewGuid();

        var actions = new List<AgvAction>();

        if (sourceNode != null)
        {
            var pickId = Guid.NewGuid();
            var pickAction = new AgvAction(pickId, ActionType.Pick, targetNode: sourceNode, correlationId) { Sequence = 0, AdditionalInfo = infoPick };
            pickAction.SetDockFrontValue(dockFrontPick);
            pickAction.SetExternalLiftHeight();
            actions.Add(pickAction);
        }

        if (targetNode != null)
        {
            var dropId = Guid.NewGuid();
            var dropAction = new AgvAction(dropId, ActionType.Drop, targetNode: targetNode, correlationId) { Sequence = sourceNode == null ? 0 : 1, AdditionalInfo = infoDrop };
            dropAction.SetDockFrontValue(dockFrontDrop);
            dropAction.SetExternalLiftHeight();
            actions.Add(dropAction);
        }

        if (actions.Count == 1)
        {
            var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId!.Value);
            if (!agv.IsInMaintenance)
            {
                throw new ActionNotAllowedInModeException(agv.CorrelationId, agv.Mode.Type.ToString());
            }
        }

        var actionGroup = new Domain.Models.ActionGroup.ActionGroup(
            correlationId,
            $"M-{DateTime.UtcNow:yyyyMMdd-tthhmm}",
            actions: actions,
            creator: creator,
            assignedAgv: agvCorrelationId,
            priority: Priority.Manual,
            isManual: creator == ActionGroupCreator.TransportOrderWebApi,
            shouldEscapeQueue: true
        ) { HandlingUnit = handlingUnit };

        var validationResult = await _actionGroupValidationService.ValidateAsync(actionGroup);
        if (!validationResult.IsValid)
        {
            throw new ActionGroupInvalidException(validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
        }

        return actionGroup;
    }
}