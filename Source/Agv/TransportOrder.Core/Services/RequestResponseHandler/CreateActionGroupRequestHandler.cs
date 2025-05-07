using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
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

    /// <inheritdoc cref="CreateActionGroupRequestHandler" />
    public CreateActionGroupRequestHandler(IActionGroupRetrievalService retrievalService,
        IActionGroupRepository actionGroupRepository,
        IActionGroupValidationService actionGroupValidationService,
        IBus bus,
        IAgvDb agvDb,
        IMapper mapper,
        ILogger<CreateActionGroupRequestHandler> logger
        )
    {
        _retrievalService = retrievalService;
        _actionGroupRepository = actionGroupRepository;
        _agvDb = agvDb;
        _mapper = mapper;
        _actionGroupValidationService = actionGroupValidationService;
        _bus = bus;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="Messages.Requests.ActionGroup.CreatePickDropActionGroup"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<CreatePickDropActionGroup> context)
    {
        (string sourceNode, string targetNode, bool dockFrontPick, bool dockFrontDrop, string? handlingUnit, Guid? agvCorrelationId, string? infoPick, string? infoDrop, bool hasPriority, var creator) = await ValidateMessageContent(context);

        var actionGroup = await CreatePickDropActionGroup(sourceNode, targetNode, dockFrontPick, dockFrontDrop, handlingUnit, agvCorrelationId, infoPick, infoDrop, hasPriority, creator);
        actionGroup.Queue();
        await _actionGroupRepository.AddAsync(actionGroup);
        
        _logger.LogOrderInformation(LogCodes.OrderCreated, agvCorrelationId.GetValueOrDefault(),
            actionGroup.CorrelationId, "Order Created", $"Order {actionGroup.OrderId} was created successfully",
            new Dictionary<string, object?>{{LogLabels.OrderId, actionGroup.OrderId } });

        _logger.LogOrderInformation(LogCodes.OrderQueued, agvCorrelationId.GetValueOrDefault(),
            actionGroup.CorrelationId, "Order Queued", $"Order {actionGroup.OrderId} was queued successfully",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });

        await _bus.Publish(new Common.Messages.ActionGroups.Events.ActionGroupCreated(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId, agvCorrelationId));
        await context.RespondAsync(new ActionGroupCreated(await _retrievalService.EnrichActionGroupWithNodesAsync(actionGroup)));
    }

    private async Task<CreatePickDropActionGroup> ValidateMessageContent(ConsumeContext<CreatePickDropActionGroup> context)
    {
        (string source, string target, _, _, _, Guid? agvCorrelationId, _, _, _, _) = context.Message;

        // validate agv existence 
        if (agvCorrelationId != null)
        {
            var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId.Value);
            if (!agv.CanStartManualPickDropAction)
            {
                throw new ActionNotAllowedInModeException(agv.CorrelationId, agv.Mode.Type.ToString());
            }
        }

        // validate source and target node
        var (sourceNode, targetNode) = await TryGetSourceAndTargetNode(source, target, agvCorrelationId);
        if (!sourceNode.IsPickStation())
        {
            throw new InvalidNodeTypeException(source, sourceNode.Type.ToString()?? "", $"{nameof(AgvGraph.NodeType.Pick)} or {nameof(AgvGraph.NodeType.PickDrop)}");
        }

        if (!targetNode.IsDropStation())
        {
            throw new InvalidNodeTypeException(target, targetNode.Type.ToString()??"", $"{nameof(AgvGraph.NodeType.Drop)} or {nameof(AgvGraph.NodeType.PickDrop)}");
        }
        return context.Message;
    }

    private async Task<(GraphNode sourceNode, GraphNode targetNode)> TryGetSourceAndTargetNode(string source, string target, Guid? agvCorrelationId)
    {
        ICollection<GraphNode> nodes;
        if (agvCorrelationId == null)
        {
            // get source and target independent of agv graph 
            nodes = await _agvDb.Graphs.GetNodesByIdsAsync([source, target]);
        }
        else
        {
            // get source and target from the graph of the given agv
            nodes = (await _agvDb.Graphs.GetNodesAsync(agvCorrelationId.Value, node => new[] {source, target}.Contains(node.Id))).ToList();
        }

        return (nodes.SelectById(source), nodes.SelectById(target));
    }

    private async Task<Domain.Models.ActionGroup.ActionGroup> CreatePickDropActionGroup(string sourceNode, string targetNode, bool dockFrontPick, bool dockFrontDrop, string? handlingUnit, Guid? agvCorrelationId, string? infoPick, string? infoDrop, bool hasPriority, ActionGroupCreator creator)
    {
        var correlationId = Guid.NewGuid();

        var pickId = Guid.NewGuid();
        var dropId = Guid.NewGuid();

        var pickAction = new AgvAction(pickId, ActionType.Pick, sourceNode, correlationId) {Sequence = 0, AdditionalInfo = infoPick};
        pickAction.SetDockFrontValue(dockFrontPick);
        pickAction.SetLiftHeight();

        var dropAction = new AgvAction(dropId, ActionType.Drop, targetNode, correlationId) {Sequence = 1, AdditionalInfo = infoDrop};
        dropAction.SetDockFrontValue(dockFrontDrop);
        dropAction.SetLiftHeight();

        var actionGroup = new Domain.Models.ActionGroup.ActionGroup(
            correlationId,
            $"M-{DateTime.UtcNow:yyyyMMdd-tthhmm}",
            actions: new List<AgvAction>
            {
                pickAction,
                dropAction
            },
            creator: creator,
            assignedAgv: agvCorrelationId,
            isManual: creator == ActionGroupCreator.TransportOrderWebApi,
            hasPriority: hasPriority
        ) {HandlingUnit = handlingUnit};

        var validationResult = await _actionGroupValidationService.ValidateAsync(actionGroup);
        if (!validationResult.IsValid)
        {
            throw new ActionGroupInvalidException(validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
        }

        return actionGroup;
    }
}